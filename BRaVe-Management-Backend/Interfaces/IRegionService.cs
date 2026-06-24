using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IRegionService
    {
        //Region section
        Task<IEnumerable<Region>> GetAllRegions();
        Task<Region?> GetRegion(int id);
        Task CreateRegion(RegionDto data, string UserId);
        Task UpdateRegion(Region data, string UserId);
        Task DeleteRegion(int id);

        //Country section
        Task<IEnumerable<Country>> GetAllCountries();
        Task<IEnumerable<Country>> GetAllCountriesByRegionId(int id);
        Task<Country?> GetCountry(string id);
        Task CreateCountry(Country data, string UserId);
        Task UpdateCountry(Country data, string UserId);
        Task DeleteCountry(string id);
    }
}
