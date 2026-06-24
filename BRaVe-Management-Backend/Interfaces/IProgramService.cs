using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using BRaVe_Portal.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IProgramService
    {     
        Task<IEnumerable<ProgramDef>> GetAllProgramsByTenant(string UserId, int TenantId);

        Task CreateProgram(string UserId, ProgramDto data);

        Task UpdateProgram(string UserId, ProgramDto data);

        Task<List<ProgramDetails>> GetProgramsByMissionAsync(int? missionId);

    }
}
