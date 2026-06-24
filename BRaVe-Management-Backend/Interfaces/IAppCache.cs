namespace BRaVe_Management_Backend.Interfaces
{
    public interface IAppCache
    {
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
        Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
                                 TimeSpan? ttl = null, CancellationToken ct = default);
        Task<bool> RemoveAsync(string key, CancellationToken ct = default);
        Task<bool> ExistsAsync(string key, CancellationToken ct = default);


        Task<long> IncrementAsync(string key, long by = 1, TimeSpan? ttl = null, CancellationToken ct = default);
        Task<long> DecrementAsync(string key, long by = 1, TimeSpan? ttl = null, CancellationToken ct = default);

    }
}
