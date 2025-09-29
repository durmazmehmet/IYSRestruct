using IYS.Application.Services.Interface;
using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IYS.Application.Services
{
    public class HybridCacheService(
        IMemoryCache memoryCache,
        IDbService dbService,
        ILogger<HybridCacheService> logger) : ICacheService
    {
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly IDbService _dbService = dbService;
        private readonly ILogger<HybridCacheService> _logger = logger;
        private readonly int _memoryCacheDurationMinutes = 60;

        public async Task<T> GetCachedDataAsync<T>(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentException("Cache anahtarı boş olamaz.", nameof(cacheKey));
            }

            if (_memoryCache.TryGetValue(cacheKey, out T cachedValue))
            {
                return cachedValue;
            }

            var tokenEntity = await _dbService.GetTokenResponseLog(cacheKey);

            if (tokenEntity == null || string.IsNullOrWhiteSpace(tokenEntity.TokenResponse))
            {
                await SetCachedHaltUntilAsync(cacheKey, tokenEntity?.HaltUntilUtc);
                return default;
            }

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(tokenEntity.TokenResponse));
                var deserialized = JsonConvert.DeserializeObject<T>(json);

                if (deserialized is null)
                {
                    await SetCachedHaltUntilAsync(cacheKey, tokenEntity.HaltUntilUtc);
                    return default;
                }

                _memoryCache.Set(cacheKey, deserialized, GetCacheEntryOptions());
                await SetCachedHaltUntilAsync(cacheKey, tokenEntity.HaltUntilUtc);

                return deserialized;
            }
            catch (FormatException formatException)
            {
                _logger.LogWarning(formatException, "{CacheKey} için veritabanındaki veri base64 formatında çözümlenemedi.", cacheKey);
                return default;
            }
        }


        public async Task SetCacheDataAsync<T>(string cacheKey, T data)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentException("Cache anahtarı boş olamaz.", nameof(cacheKey));
            }

            var serializedData = JsonConvert.SerializeObject(data);
            var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(serializedData));

            _memoryCache.Set(cacheKey, data, GetCacheEntryOptions());
            await SetCachedHaltUntilAsync(cacheKey, null);

            var affectedRows = await _dbService.UpdateTokenResponseLog(new Models.Response.Schedule.TokenResponseLog
            {
                IysCode = cacheKey,
                TokenResponse = base64Data,
                HaltUntilUtc = null
            });

            if (affectedRows <= 0)
            {
                _logger.LogWarning("{CacheKey} anahtarı için token veritabanına kaydedilemedi.", cacheKey);
            }
        }


        public Task<T> GetCachedHashDataAsync<T>(string hashKey, string key)
        {
            return GetCachedDataAsync<T>(ComposeKey(hashKey, key));
        }

        public Task SetCacheHashDataAsync<T>(string hashKey, string key, T data)
        {
            return SetCacheDataAsync(ComposeKey(hashKey, key), data);
        }

        public Task<DateTime?> GetCachedHaltUntilAsync(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentException("Cache anahtarı boş olamaz.", nameof(cacheKey));
            }

            return Task.FromResult(_memoryCache.TryGetValue<DateTime?>(ComposeHaltKey(cacheKey), out var value)
                ? value
                : null);
        }

        public Task SetCachedHaltUntilAsync(string cacheKey, DateTime? haltUntilUtc)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentException("Cache anahtarı boş olamaz.", nameof(cacheKey));
            }

            var haltKey = ComposeHaltKey(cacheKey);

            if (haltUntilUtc.HasValue)
            {
                _memoryCache.Set(haltKey, haltUntilUtc, GetCacheEntryOptions());
            }
            else
            {
                _memoryCache.Remove(haltKey);
            }

            return Task.CompletedTask;
        }

        private MemoryCacheEntryOptions GetCacheEntryOptions()
        {
            return new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_memoryCacheDurationMinutes)
            };
        }

        private static string ComposeKey(string hashKey, string key)
        {
            if (string.IsNullOrWhiteSpace(hashKey))
            {
                return key;
            }

            return string.IsNullOrWhiteSpace(key) ? hashKey : $"{hashKey}:{key}";
        }

        private static string ComposeHaltKey(string cacheKey) => string.Concat(cacheKey, ":halt");
    }
}
