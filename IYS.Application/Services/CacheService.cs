using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;


namespace IYS.Application.Services
{
    public class CacheService : ICacheService
    {
        private readonly ILogger<CacheService> _logger;
        private readonly IDatabase _cache;

        public CacheService(IConnectionMultiplexer redis, ILogger<CacheService> logger)
        {
            _logger = logger;
            _cache = redis.GetDatabase();
        }

        public ResponseBase<bool> CheckCache()
        {
            var response = new ResponseBase<bool>();

            if (_cache == null)
            {
                response.AddMessage("Redis","Redis cache nesnesi null.");
                response.Error();
                return response;
            }

            var _multiplexer = _cache.Multiplexer;

            if (!_multiplexer.IsConnected)
            {
                response.AddMessage("Redis", "Redis bağlantısı başarısız (IsConnected=false)");
                response.Error();
                return response;
            }

            var cacheStatus = _multiplexer.GetStatus();

            if (!cacheStatus.Contains("ConnectedEstablished") || cacheStatus.Contains("slave") || cacheStatus.Contains("replica"))
            {
                response.AddMessage("Redis", "Redis bağlantısı sağlıksız veya slave.");
                response.Error();
                return response;
            }

            return response;
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
