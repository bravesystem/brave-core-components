namespace BRaVe_Management_Backend.Interfaces
{
    public interface IJwtService
    {
        Task<string> CreateTokenAsync(CancellationToken ct = default);
    }
}
