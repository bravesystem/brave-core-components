using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using BRaVe_Portal.Models.ViewModels;
using System.Threading.Tasks;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDashboardService
    {
        Task<IEnumerable<DashboardRowDto>> GetDashboardDataAsync(int tenantId, int? programId = null, int? activityId = null, DateTime? startDate = null, DateTime? endDate = null);

        Task<IEnumerable<DashboardSurveyDetailDto>> GetDashboardSurveyDetailsAsync(string? householdId, int? individualId);

        Task<HouseholdDto?> GetHouseholdByIdAsync(string householdId, int tenantId);

        Task<IEnumerable<HouseholdMemberDto>> GetHouseholdMembersAsync(string householdId);

        Task<DashboardGraphDto> GetDashboardGraphDataAsync(int? tenantId, int? programId, DateTime? startDate, DateTime? endDate);


    }

}
