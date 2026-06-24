using BRaVe_Portal.Models;

namespace BRaVe_Portal.Interfaces
{
    public interface ILookupService
    {
        CoreLookups CoreLookups { get; }
        Task<CoreLookups> GetCoreLookups(string Lang);
        Task<List<AdministrativeLevel>> GetAdminLevels();
        Task<Dictionary<int, List<Location>>> GetAllLocationsByLevel();
    }
}
