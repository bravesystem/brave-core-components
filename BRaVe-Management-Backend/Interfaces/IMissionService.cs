using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IMissionService
    {
        Task<IEnumerable<Mission>> GetAllMissions();

        Task<IEnumerable<Mission>> GetAllMissionsByCountry(string id);
        Task<Mission?> GetMission(int id);
        Task<string> CreateMission(MissionDto data, string UserId);
        Task UpdateMission(Mission data, string UserId);
        Task DeleteMission(int id);

        Task<Mission?> GetUserMission(string UserId);

        Task BootstrapMission(string tenantCode);
    }
}
