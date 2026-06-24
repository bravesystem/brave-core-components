using BRaVe_Management_Backend.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace BRaVe_Management_Backend.Services
{
    public sealed class RedisCacheOptions
    {
        public string KeyPrefix { get; set; } = "doc:";
        public JsonSerializerOptions Json { get; set; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    public class RedisCache : IAppCache
    {
        private readonly IConnectionMultiplexer _mux;
        private readonly IDatabase _db;
        private readonly RedisCacheOptions _opts;

        public RedisCache(IConnectionMultiplexer mux, RedisCacheOptions? options = null, int db = -1)
        {
            _mux = mux;
            _db = _mux.GetDatabase(db);
            _opts = options ?? new RedisCacheOptions();
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var val = await _db.StringGetAsync(P(key)).ConfigureAwait(false);
            if (!val.HasValue) return default;
            return JsonSerializer.Deserialize<T>(val!, _opts.Json);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
        {
            if (ttl == null)
                ttl = TimeSpan.FromHours(1);

            var json = JsonSerializer.Serialize(value, _opts.Json);
            await _db.StringSetAsync(P(key), json, ttl).ConfigureAwait(false);
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
                                              TimeSpan? ttl = null, CancellationToken ct = default)
        {
            var cached = await GetAsync<T>(key, ct).ConfigureAwait(false);
            if (cached is not null) return cached;

            var value = await factory(ct).ConfigureAwait(false);
            await SetAsync(key, value, ttl, ct).ConfigureAwait(false);
            return value!;
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken ct = default)
            => await _db.KeyDeleteAsync(P(key)).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
            => await _db.KeyExistsAsync(P(key)).ConfigureAwait(false);

        public async Task<long> IncrementAsync(string key, long by = 1, TimeSpan? ttl = null, CancellationToken ct = default)
        {
            var full = P(key);
            var val = await _db.StringIncrementAsync(full, by).ConfigureAwait(false);
            if (ttl.HasValue) await _db.KeyExpireAsync(full, ttl).ConfigureAwait(false);
            return val;
        }

        public async Task<long> DecrementAsync(string key, long by = 1, TimeSpan? ttl = null, CancellationToken ct = default)
        {
            var full = P(key);
            var val = await _db.StringDecrementAsync(full, by).ConfigureAwait(false);
            if (ttl.HasValue) await _db.KeyExpireAsync(full, ttl).ConfigureAwait(false);
            return val;
        }

        private string P(string key) => string.Concat(_opts.KeyPrefix, key);

    }
}
