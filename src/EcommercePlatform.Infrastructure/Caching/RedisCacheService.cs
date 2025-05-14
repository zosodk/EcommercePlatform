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
        private static readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

        public RedisCacheService(IConnectionMultiplexer redisConnection, ILogger<RedisCacheService> logger)
        {
            _redisDb = redisConnection?.GetDatabase() ?? throw new ArgumentNullException(nameof(redisConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return default;
            try
            {
                RedisValue redisValue = await _redisDb.StringGetAsync(key);
                if (redisValue.IsNullOrEmpty) { _logger.LogInformation("Cache miss for key: {Key}", key); return default; }
                _logger.LogInformation("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(redisValue.ToString(), _serializerOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error getting from Redis for key: {Key}", key); return default; }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (value == null) { await RemoveAsync(key); return; } // Or store a specific null marker
            try
            {
                string jsonValue = JsonSerializer.Serialize(value, _serializerOptions);
                await _redisDb.StringSetAsync(key, jsonValue, expiry);
                _logger.LogInformation("Cached data for key: {Key}, Expiry: {Expiry}", key, expiry?.ToString() ?? "N/A");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error setting to Redis for key: {Key}", key); }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            try
            {
                await _redisDb.KeyDeleteAsync(key);
                _logger.LogInformation("Removed key from Redis: {Key}", key);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error removing from Redis for key: {Key}", key); }
        }
    }