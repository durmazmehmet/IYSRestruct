using IYS.Application.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace IYS.Application.Services
{
    public class HybridCacheService(IMemoryCache memoryCache, IDbService dbService) : ICacheService
    {
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly IDbService _dbService = dbService;
        private readonly int _memoryCacheDurationMinutes = 60;

        public async Task<T> GetCachedDataAsync<T>(string cacheKey)
        {
            if (_memoryCache.TryGetValue(cacheKey, out T cachedValue))
            {
                return cachedValue;
            }

            var tokenEntity = await _dbService.GetTokenResponseLog(cacheKey);

            if (string.IsNullOrEmpty(tokenEntity))
            {
                return default;
            }

            // Base64 decode
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(tokenEntity));
            var deserialized = JsonConvert.DeserializeObject<T>(json);

            _memoryCache.Set(cacheKey, deserialized, TimeSpan.FromMinutes(_memoryCacheDurationMinutes));

            return deserialized;
        }


        public async Task SetCacheDataAsync<T>(string cacheKey, T data)
        {
            var serializedData = JsonConvert.SerializeObject(data);
            var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(serializedData));

            _memoryCache.Set(cacheKey, data, TimeSpan.FromMinutes(_memoryCacheDurationMinutes));

            await _dbService.UpdateTokenResponseLog(new Models.Response.Schedule.TokenResponseLog
            {
                IysCode = cacheKey,
                TokenResponse = base64Data
            });
        }


        public Task<T> GetCachedHashDataAsync<T>(string hashKey, string key)
        {
            return GetCachedDataAsync<T>(key);
        }

        public Task SetCacheHashDataAsync<T>(string hashKey, string key, T data)
        {
            return SetCacheDataAsync(key, data);
        }
    }
}
