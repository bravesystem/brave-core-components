using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IUserMissionMappingService
    {
        Task<IEnumerable<UserMissionRequest>> GetAllUserRequests();

        Task<bool> GetIfUserExists(string userId);
        Task<UserMissionRequest?> GetUserRequestById(string userid);

        Task CreateUserRequest(UserRequestDto data);

        Task AssignTenantToUser(UserMissionResultDto userMission, string UpdatedBy);

        Task CreateMissionPreAssignment(MissionPreAssignmentDto data);

        Task<List<string>> CreateMissionBulkPreAssignment(List<MissionPreAssignmentDto> data);
        Task<IEnumerable<PreAssignedUserDto>> GetPendingPreAssignedUsers(int tenantId);



    }
}
