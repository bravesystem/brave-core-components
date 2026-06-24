using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IPreferenceService
    {

        /// <summary>
        /// Retrieves all preferences.
        /// </summary>
        Task<IEnumerable<MissionPreferences>> GetAllMissionPreferences(int tenantId);
        Task CreatePreference(Preferences newPreference);
        Task UpdatePreference(Preferences updatedPreference);

        Task<IEnumerable<Preferences>> GetDefaultPreferences();

        // Mission related preferences
        Task AddPreferenceToMission(MissionPreferences newPreference);

        Task UpdateMissionPreference(List<MissionPreferences> updatedList);

        Task<IEnumerable<MissionPreferences>> GetAllProgramPreferences(int programId);

        Task<IEnumerable<MissionPreferences>> GetPreferenceById(int PreferenceId, int tenantId);
     
        //Task CreatePreferenceAsync(List<MissionPreferences> createdList);

        Task<int> CreatePreferenceAsync(int tenantId, string userId);
    }
}
