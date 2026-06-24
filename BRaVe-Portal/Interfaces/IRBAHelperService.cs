using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.ViewModels;
using BRaVe_Portal.Pages.Assessments;

namespace BRaVe_Portal.Interfaces
{
    public interface IRBAHelperService
    {
        Task<bool> AddPersonalData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingPersonalData data);
        Task<bool> UpdatePersonalData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingPersonalData data);
        Task<bool> DeletePersonalData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingPersonalData data);
        Task<RiskBenefitAssessmentViewModel> GetAssessment(string Identifier, int assessmentId,  string languageCode="en", bool UseCache=true);
        Task<bool> UpdateDataProcessing(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessing data);
        Task<bool> UpdateLawBases(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingLawfulBasis data);
        
        Task<bool> UpdateRetention(RiskBenefitAssessmentViewModel assessment, string Identifier, DataProcessingRetention data);
        Task<(bool,string)> AddDataCollaborator(RiskBenefitAssessmentViewModel assessment, string Identifier, DataProcessingCollaborator data);
        Task<bool> UpdateDataCollaborator(RiskBenefitAssessmentViewModel assessment, string Identifier, DataProcessingCollaborator data);
        Task<bool> DeleteDataCollaborator(RiskBenefitAssessmentViewModel assessment, string Identifier, DataProcessingCollaborator data);
        Task<bool> AddDataSubject(RiskBenefitAssessmentViewModel assessment, string Identifier, DataProcessingDataSubject data);
        Task<bool> UpdateDataSubject(RiskBenefitAssessmentViewModel assessment, string Identifier, DataProcessingDataSubject data);
        Task<bool> DeleteDataSubject(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSubject data);
        Task<bool> AddDisclosureRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataDisclosureRecipient data);
        Task<bool> UpdateDisclosureRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataDisclosureRecipient data);
        Task<bool> DeleteDisclosureRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataDisclosureRecipient data);
        Task<bool> AddDataSharingRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSharingRecipient data);
        Task<bool> UpdateDataSharingRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSharingRecipient data);
        Task<bool> DeleteDataSharingRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSharingRecipient data);
        Task<bool> AddDataSecurityMeasure(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSecurityMeasure data);
        Task<bool> UpdateDataSecurityMeasure(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSecurityMeasure data);
        Task<bool> DeleteDataSecurityMeasure(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSecurityMeasure data);
        Task<bool> AddSourceOfData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSourceOfData data);
        Task<bool> UpdateSourceOfData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSourceOfData data);
        Task<bool> DeleteSourceOfData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSourceOfData data);
        Task<bool> AddDataOutput(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataOutput data);
        Task<bool> UpdateDataOutput(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataOutput data);
        Task<bool> DeleteDataOutput(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataOutput data);
        Task<bool> UpdateRbaStatus(RiskBenefitAssessmentViewModel Assessment, string Identifier, StatusChangeDto data);
        Task<(bool,string)> SaveCoMDecision(string Identifier, DecisionDto data);
        Task<Decision?> GetCurrentCoMDecision(string Identifier, int assessmentId);
        Task<(bool, string)> SaveRecommendation(string Identifier,  RecommendationDto data);
        Task<Recommendation?> GetCurrentRecommendation(string Identifier, bool isleg, int assessmentId);
        Task<Decision?> GetCurrentPmDecision(string Identifier, int assessmentId);
        Task<(bool, string)> SavePmDecision(string Identifier, DecisionDto data);
    }
}
