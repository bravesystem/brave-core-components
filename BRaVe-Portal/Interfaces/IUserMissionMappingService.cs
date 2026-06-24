using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models;

namespace BRaVe_Portal.Interfaces
{
    public interface IUserMissionMappingService
    {
        Task<IEnumerable<UserMissionRequest>> GetAllUserRequests();

        Task<UserMissionRequest?> GetUserRequestById(int id);

        Task CreateUserRequest(UserRequestDto data);

        Task AssignTenantToUser(UserMissionResultDto userMission, string UpdatedBy);
    }
}
