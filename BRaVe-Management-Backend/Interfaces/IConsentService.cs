using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using static BRaVe_Management_Backend.Services.SqlClaimService;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IConsentService
    {
        
        Task<IEnumerable<Consent>> GetAllConsents(int ProgramId);
         Task<Consent?> GetConsentById(int id); //
         Task UpdateConsent(int id, ConsentDto data, string UserId, string languageCode);  //

        Task CreateConsent(ConsentDto data, int TenantId, string UserId);
        //Task GetConsentByConsentId(int consentId);
        Task AddOrUpdateTranslationAsync(ConsentTranslationDto translation, string userId);
        Task<IEnumerable<ConsentTranslationDto>> GetAllTranslationsAsync(int ProgramId, int consentId);
        Task DeleteTranslationAsync(int ProgramId,int consentId, string languageCode);
    }
}
