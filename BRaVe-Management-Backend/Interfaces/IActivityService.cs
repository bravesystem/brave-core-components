using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using BRaVe_Portal.Models.ViewModels;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IActivityService
    {

        // <summary>
        /// Retrieves all activities.
        /// </summary>
        Task<IEnumerable<RegistrationActivity>> GetAllActivities(int ProgramId);

        /// <summary>
        /// Retrieves a specific activity by its ID.
        /// </summary>
        Task<RegistrationActivity> GetActivityById(int id);
        Task<IEnumerable<ActivityDataPoint>> GetDataPointsForActivity(int activityId);

        /// <summary>
        /// Creates a new activity.
        /// </summary>
        Task CreateActivity(RegistrationActivity newActivity);

        /// <summary>
        /// Updates an existing activity.
        /// </summary>
        Task UpdateActivity(RegistrationActivity updatedActivity);
        Task AttachDataPointAsync(int activityId, int dataPointId, int datapointType, RequiredDto value);
        Task DetachDataPointAsync(int activityId, int dataPointId);
        Task AttachSurveyAsync(int activityId, int surveyId, RequiredDto value);
        Task DetachSurveyAsync(int activityId, int surveyId);
        Task<IEnumerable<ActivitySurveys>> GetSurveysForActivity(int activityId);
        Task AttachConsentAsync(int activityId, int consentId, int consentType, RequiredDto value);
        Task<IEnumerable<ActivityConsent>> GetConsentForActivity(int tenantId,int activityId, string languageCode);

        Task RemoveAttachedSurveyAsync(int activityId, int surveyId);

        Task RemoveAttachedConsentAsync(int activityId, int consentId);

        Task AttachPreferenceAsync(int activityId, List<MissionPreferences> createdList);
        Task<IEnumerable<ActivityPreference>> GetAttachedPreferences(int PreferenceId, int TenantId);

        Task<IEnumerable<ActivityEnumerator>> GetEnumeratorsForActivity(int activityId);

        Task RemovePreferenceAsync(int activityId, int preferenceId);

        Task RemoveEnumeratorAsync(int activityId, string enumeratorId);

        Task AttachDistributionsAsync(int activityId, int distributorId, int distributionType, int EnrollmentMode, bool PhotoConfirmation,bool BiometricVerification, RequiredDto value);
        Task DetachDistributionsAsync(int activityId, int distributorId);

        Task<IEnumerable<ActivityDistributions>> GetDistributionsForActivity(int activityId);

        Task RemoveDistributionsAsync(int activityId, int distributorId);

        Task Validate(int ActivityId, int TenantId, string UpdatedByUserId);

        Task Invalidate(int ActivityId, int TenantId, string UpdatedByUserId);
        Task AttachEnumeratorsBulkAsync(int activityId, List<string> enumeratorCodes, int tenantId);
    }
}
