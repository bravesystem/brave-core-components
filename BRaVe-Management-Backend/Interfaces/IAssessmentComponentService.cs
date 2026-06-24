using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IAssessmentComponentService
    {
        Task UpdateRBAStatus(StatusChangeDto data, int TenantId, string UserId);
        Task AddPersonalData(DataProcessingPersonalData data, int TenantId, string UserId);
        Task AddDataProcessing(DataProcessing data, int TenantId, string UserId);
        Task AddLawfulBasis(DataProcessingLawfulBasis data, int TenantId, string UserId);
        Task AddDataRetention(DataProcessingRetention data, int TenantId, string UserId);
        Task AddCollaborator(DataProcessingCollaborator data, int TenantId, string UserId);
        Task AddDataSubjects(DataProcessingDataSubject data, int TenantId, string UserId);
        Task AddDataDisclosureRecipients(DataProcessingDataDisclosureRecipient data, int TenantId, string UserId);
        Task AddDataSharingRecipients(DataProcessingDataSharingRecipient data, int TenantId, string UserId);
        Task AddSecurityMeasures(DataProcessingSecurityMeasure data, int TenantId, string UserId);
        Task AddSourcesOfData(DataProcessingSourceOfData data, int TenantId, string UserId);
        Task AddDataOutputs(DataProcessingDataOutput data, int TenantId, string UserId);


        Task EditCollaborator(DataProcessingCollaborator data, string UserId);
        Task EditDataSubjects(DataProcessingDataSubject data, string UserId);
        Task EditPersonalData(DataProcessingPersonalData data, string UserId);
        Task EditDisclosureRecipients(DataProcessingDataDisclosureRecipient data, string UserId);
        Task EditSecurityMeasures(DataProcessingSecurityMeasure data, string UserId);
        Task EditSourcesOfData(DataProcessingSourceOfData data, string UserId);
        Task EditSharingRecipients(DataProcessingDataSharingRecipient data, string UserId);
        Task EditDataOutputs(DataProcessingDataOutput data, string UserId);



        Task DeleteCollaborator(int assessmentId, int partnerId, string UserId);
        Task DeleteDataSubjects(int assessmentId, int subjectId, string UserId);
        Task DeletePersonalData(int assessmentId, int personalcategoryId, string UserId);
        Task DeleteDisclosureRecipients(int assessmentId, int recipientId, string UserId);
        Task DeleteSecurityMeasures(int assessmentId, int measureId, string UserId);
        Task DeleteSourcesOfData(int assessmentId, string UserId);
        Task DeleteSharingRecipients(int assessmentId, int recipientId, string UserId);
        Task DeleteDataOutputs(int assessmentId, int outputId, string UserId); 


    }
}
