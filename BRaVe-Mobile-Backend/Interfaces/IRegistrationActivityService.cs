using BRaVe_Mobile_Backend.Models;
using BRaVe_Mobile_Backend.Models.data_payload;

namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface IRegistrationActivityService
    {
        Task<EnrollmentResponse> fetchEnrollment( string activityId, int tenantId, string deviceId, EnrollmentRequest request);
        Task<EnrollmentResponseBulk> fetchEnrollmentBulk( string activityId, int tenantId, string enumerator, string deviceId, EnrollmentRequestBulk request);
        Task<RegistrationActivity> GetActivity(string deviceId,string activityId, int tenantId, string language);
        Task SaveAsync(RegistrationActivityStaging stagingRecord);
        Task<List<BiometricVerificationResponse>> VerifyTemplates(int tenantId, Guid jobId, string activityId, string deviceId, int distributionId, List<BiometricVerificationRequest> data);

        Task<FlaggedData> DownloadFlaggedData(int tenantId,string partitionCode, string deviceId);
    }
}
