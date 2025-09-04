using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;


namespace IYSIntegration.Application.Services
{

    public interface ICacheService
    {
        public Task<T> GetCachedDataAsync<T>(string cacheKey);
        public Task SetCacheDataAsync<T>(string cacheKey, T data);
        public Task<T> GetCachedHashDataAsync<T>(string hashKey, string Key);
        public Task SetCacheHashDataAsync<T>(string hashKey, string Key, T data);
    }
    public class CacheService : ICacheService
    {
        private readonly ILogger<CacheService> _logger;
        private readonly IDatabase _cache;

        public CacheService(IConnectionMultiplexer redis, ILogger<CacheService> logger)
        {
            _logger = logger;
            _cache = redis.GetDatabase();
        }

        public async Task<T> GetCachedDataAsync<T>(string cacheKey)
        {
            var cachedData = await _cache.StringGetAsync(cacheKey);

            if (!cachedData.HasValue)
            {
                _logger.LogWarning($"Cache'de veri bulunamadı: {cacheKey}");
                return default;
            }

            _logger.LogInformation($"Cache'den veri alındı: {cacheKey}");
            return JsonConvert.DeserializeObject<T>(cachedData);
        }

        public async Task SetCacheDataAsync<T>(string cacheKey, T data)
        {
            var serializedData = JsonConvert.SerializeObject(data);
            await _cache.StringSetAsync(cacheKey, serializedData);
            _logger.LogInformation($"Cache'e veri eklendi: {cacheKey}");
        }

        public async Task SetCacheHashDataAsync<T>(string hashKey, string Key, T data)
        {
            var serializedData = JsonConvert.SerializeObject(data);
            await _cache.HashSetAsync(hashKey, Key, serializedData);
            _logger.LogInformation($"Cache'e veri eklendi: {hashKey} {Key}");
        }

        public async Task<T> GetCachedHashDataAsync<T>(string hashKey, string Key)
        {
            RedisValue cachedResponse = await _cache.HashGetAsync(hashKey, Key);

            if (!cachedResponse.HasValue)
            {
                _logger.LogWarning($"Cache'de veri bulunamadı: {hashKey} {Key}");
                return default;
            }

            _logger.LogInformation($"Cache'den veri alındı: {hashKey} {Key}");
            return JsonConvert.DeserializeObject<T>(cachedResponse)!;
        }
    }
}
