namespace IYS.Application.Services.Interface
{
    public interface ICacheService
    {
        public Task<T> GetCachedDataAsync<T>(string cacheKey);
        public Task SetCacheDataAsync<T>(string cacheKey, T data);
        public Task<T> GetCachedHashDataAsync<T>(string hashKey, string Key);
        public Task SetCacheHashDataAsync<T>(string hashKey, string Key, T data);
        public Task<DateTime?> GetCachedHaltUntilAsync(string cacheKey);
        public Task SetCachedHaltUntilAsync(string cacheKey, DateTime? haltUntilUtc);
    }
}
