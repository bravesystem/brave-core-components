using BRaVe_Management_Backend.DTOs.claim_session;
using BRaVe_Management_Backend.DTOs.PrintService;
using System.Threading.Tasks;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IPrintService
    {

        //FOR CARD PRINTING DESKTOP APP
        Task<PrintDeviceClaimResult> ClaimPrintAsync(string sessionCode, string deviceId);

        Task<RefreshResponse> RefreshAsync(string refreshToken);

        Task<List<CardPrintQueueItem>> LoadPrintQueueAsync(int tenantId, List<string>? householdIds, string? activityCode, string deviceId, string user);

        Task SyncPrintedRecordsAsync(int tenantId, List<string> householdIds, string printedBy);

        //FOR WEB PORTAL 

        Task<List<PrintedCardSummaryDto>> GetPrintedRecordsAsync(int tenantId, DateTime? startDate, DateTime? endDate);
        Task AuthorizeReprintAsync(int cardId, string authorizedBy, string reason);
        Task UnlockPrintQueueRecordsAsync(List<string> householdIds, string currentUser);
    }
}
