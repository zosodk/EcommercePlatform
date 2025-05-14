using EcommercePlatform.Application.Interfaces.Services;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    namespace EcommercePlatform.Infrastructure.Caching;

    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisCacheService> _logger;
        private static readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            // Add other default options if needed
        };

        public RedisCacheService(IConnectionMultiplexer redisConnection, ILogger<RedisCacheService> logger)
        {
            _redisDb = redisConnection?.GetDatabase() ?? throw new ArgumentNullException(nameof(redisConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Cache key cannot be null or whitespace for GetAsync.");
                return default;
            }
            try
            {
                RedisValue redisValue = await _redisDb.StringGetAsync(key);
                if (redisValue.IsNullOrEmpty)
                {
                    _logger.LogInformation("Cache miss for key: {CacheKey}", key);
                    return default;
                }
                _logger.LogInformation("Cache hit for key: {CacheKey}", key);
                return JsonSerializer.Deserialize<T>(redisValue.ToString(), _serializerOptions);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON Deserialization error for cache key: {CacheKey}. Value: {RedisValue}", key, await _redisDb.StringGetAsync(key));
                // Consider removing the corrupted key: await RemoveAsync(key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from Redis for key: {CacheKey}", key);
                return default; // Protect app from cache failures
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Cache key cannot be null or whitespace for SetAsync.");
                return;
            }
            if (value == null)
            {
                _logger.LogWarning("Attempted to set null value in cache for key: {CacheKey}. Removing key instead.", key);
                await RemoveAsync(key); // Or store a specific marker for null if nulls are meaningful in cache
                return;
            }

            try
            {
                string jsonValue = JsonSerializer.Serialize(value, _serializerOptions);
                bool success = await _redisDb.StringSetAsync(key, jsonValue, expiry);
                if(success)
                    _logger.LogInformation("Data cached for key: {CacheKey}, Expiry: {Expiry}", key, expiry?.ToString() ?? "N/A");
                else
                    _logger.LogWarning("Failed to set cache for key: {CacheKey} (StringSetAsync returned false)", key);

            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON Serialization error for cache key: {CacheKey} during SetAsync.", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting data in Redis for key: {CacheKey}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Cache key cannot be null or whitespace for RemoveAsync.");
                return;
            }
            try
            {
                bool removed = await _redisDb.KeyDeleteAsync(key);
                if(removed)
                    _logger.LogInformation("Cache key removed: {CacheKey}", key);
                else
                    _logger.LogInformation("Cache key not found for removal or already removed: {CacheKey}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing key from Redis: {CacheKey}", key);
            }
        }
    }