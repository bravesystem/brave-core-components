namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface ISecretProvider
    {
        string GetSecretAsync(string name, CancellationToken ct = default);
    }
}
