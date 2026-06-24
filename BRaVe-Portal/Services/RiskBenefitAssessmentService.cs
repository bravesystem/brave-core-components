using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.ViewModels;
using BRaVe_Portal.Pages.Assessments;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Portal.Services
{
    public class RiskBenefitAssessmentService : IRBAHelperService
    {

        private readonly ILogger<RiskBenefitAssessmentService> _logger;

        private readonly IAppCache _cache;

        private readonly IRestApiService _api;

        public RiskBenefitAssessmentService(ILogger<RiskBenefitAssessmentService> logger,  IAppCache cache, IRestApiService api) {

            _logger = logger;
            _cache = cache;
            _api = api;

        }

        public string GetKey(string identifier, int assessment)
        {
            return $"rba:{identifier}:{assessment}";
        }
        public async Task<RiskBenefitAssessmentViewModel> GetAssessment(string Identifier,int assessmentId, string languageCode="en", bool UseCache = true)
        {
            var key = GetKey(Identifier, assessmentId);

            RiskBenefitAssessmentViewModel Assessment = null;


            try
            {

                if (await _cache.ExistsAsync(key) && UseCache)
                {
                    Assessment = await _cache.GetAsync<RiskBenefitAssessmentViewModel>(key);
                }
                else
                {
                    Assessment = await _api.GetAsync<RiskBenefitAssessmentViewModel>($"v1/RiskBenefitAssessments/{assessmentId}/{languageCode}");
                }


                await _cache.SetAsync(key, Assessment);

                return Assessment;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get assessment {AssessmentId}", assessmentId);
                throw;

            }
            
        }



        public async Task<bool> UpdateDataProcessing(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessing data)
        {

            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessing, object>(
                    "v1/DataProcessingComponents/dataprocessing", data);

                data.ProgamManagerName = Assessment.DataProcessing.ProgamManagerName;

                data.ProgamManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                Assessment.DataProcessing = data;

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }

        }



        public async Task<bool> UpdateLawBases(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingLawfulBasis data)
        {

            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingLawfulBasis, object>(
                    "v1/DataProcessingComponents/lawfulbasis", data);

                Assessment.LawfulBasis = data;

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> UpdateRetention(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingRetention data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingRetention, object>(
                    "v1/DataProcessingComponents/dataretention", data);

                Assessment.Retention = data;

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddPersonalData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingPersonalData data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingPersonalData, object>(
                    "v1/DataProcessingComponents/personaldata", data);

                if (Assessment.PersonalData == null)
                    Assessment.PersonalData = new List<DataProcessingPersonalData>();

                Assessment.PersonalData.Add( data);

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public async Task<bool> UpdatePersonalData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingPersonalData data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PutJsonAsync<DataProcessingPersonalData, object>(
                    $"v1/DataProcessingComponents/personaldata/{data.AssessmentId}/{data.PersonalDataCategoryId}",
                data);

                if (Assessment.PersonalData == null)
                    Assessment.PersonalData = new List<DataProcessingPersonalData>();

                var existing = Assessment.PersonalData.FirstOrDefault(c => c.PersonalDataCategoryId == data.PersonalDataCategoryId);
                if (existing != null)
                {
                    existing.IsSpecialCategory = data.IsSpecialCategory;
                    existing.PurposeOfDataCollection = data.PurposeOfDataCollection;
                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

                
        public async Task<bool> DeletePersonalData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingPersonalData data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.DeleteAsync($"v1/DataProcessingComponents/personaldata/{data.AssessmentId}/{data.PersonalDataCategoryId}");

                if (Assessment.PersonalData == null)
                    Assessment.PersonalData = new List<DataProcessingPersonalData>();

                var PersonalData = Assessment.PersonalData
                    .FirstOrDefault(c => c.AssessmentId == data.AssessmentId && c.PersonalDataCategoryId == data.PersonalDataCategoryId);
                if (PersonalData != null)
                {
                    Assessment.PersonalData.Remove(PersonalData);
                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }



        public async Task<(bool, string)> AddDataCollaborator(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingCollaborator data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingCollaborator, object>(
                    "v1/DataProcessingComponents/collaborators", data);

                if (Assessment.Collaborators == null)
                    Assessment.Collaborators = new List<DataProcessingCollaborator>();

                Assessment.Collaborators.Add(data);

                await _cache.SetAsync(key, Assessment);

                return (true,"");

            }
            catch (HttpRequestException ex)
            {
                string message = string.Empty;

                if (ex.Message.Contains("DuplicatePartner"))
                {
                    message = "This partner is already added as a collaborator.";
                }
                else
                {
                    message = "Unable to add collaborator due to an unexpected error.";
                }

                return (false,message);

            }
            catch (Exception)
            {
                return (false, "Unable to add collaborator due to an unexpected error.");
            }
        }

        public async Task<bool> UpdateDataCollaborator(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingCollaborator data)
        {

            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PutJsonAsync<DataProcessingCollaborator, object>(
                   $"v1/DataProcessingComponents/collaborators/{data.AssessmentId}/{data.PartnerId}",
                   data);

                if (Assessment.Collaborators == null)
                    Assessment.Collaborators = new List<DataProcessingCollaborator>();

                var existing = Assessment.Collaborators.FirstOrDefault(c => c.PartnerId == data.PartnerId);
                if (existing != null)
                {
                    existing.PartnerOrganizationName = data.PartnerOrganizationName;
                    existing.PartnerOrganizationContacts = data.PartnerOrganizationContacts;
                    existing.Description = data.Description;
                }

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDataCollaborator(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingCollaborator data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.DeleteAsync($"v1/DataProcessingComponents/collaborators/{data.AssessmentId}/{data.PartnerId}");

                if (Assessment.Collaborators == null)
                    Assessment.Collaborators = new List<DataProcessingCollaborator>();

                var collaborator = Assessment.Collaborators
                    .FirstOrDefault(c => c.AssessmentId == data.AssessmentId && c.PartnerId == data.PartnerId);
                if (collaborator != null)
                {
                    Assessment.Collaborators.Remove(collaborator);
                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddDataSubject(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSubject data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingDataSubject, object>(
                    "v1/DataProcessingComponents/datasubjects", data);

                if (Assessment.Subjects == null)
                    Assessment.Subjects = new List<DataProcessingDataSubject>();

                Assessment.Subjects.Add(data);

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateDataSubject(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSubject data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PutJsonAsync<DataProcessingDataSubject, object>(
                    $"v1/DataProcessingComponents/datasubjects/{data.AssessmentId}/{data.SubjectTypeId}",
                    data);

                if (Assessment.Subjects == null)
                    Assessment.Subjects = new List<DataProcessingDataSubject>();

                // Update cache & in-memory model
                var existing = Assessment.Subjects.FirstOrDefault(c => c.SubjectTypeId == data.SubjectTypeId);
                if (existing != null)
                {
                    existing.IsVulnerableGroup = data.IsVulnerableGroup;

                    await _cache.SetAsync(key, Assessment);

                }
                
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDataSubject(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSubject data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.DeleteAsync($"v1/DataProcessingComponents/datasubjects/{data.AssessmentId}/{data.SubjectTypeId}");


                if (Assessment.Subjects == null)
                    Assessment.Subjects = new List<DataProcessingDataSubject>();

                var subject = Assessment.Subjects
                    .FirstOrDefault(c => c.AssessmentId == data.AssessmentId && c.SubjectTypeId == data.SubjectTypeId);
                if (subject != null)
                {
                    Assessment.Subjects.Remove(subject);
                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddDisclosureRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataDisclosureRecipient data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingDataDisclosureRecipient, object>(
                    "v1/DataProcessingComponents/disclosurerecipients", data);

                if (Assessment.DisclosureRecipients == null)
                    Assessment.DisclosureRecipients = new List<DataProcessingDataDisclosureRecipient>();

                Assessment.DisclosureRecipients.Add(data);

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateDisclosureRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataDisclosureRecipient data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PutJsonAsync<DataProcessingDataDisclosureRecipient, object>(
                    $"v1/DataProcessingComponents/disclosurerecipients/{data.AssessmentId}/{data.RecipientId}",
                    data);

                if (Assessment.DisclosureRecipients == null)
                    Assessment.DisclosureRecipients = new List<DataProcessingDataDisclosureRecipient>();

                var existing = Assessment.DisclosureRecipients.FirstOrDefault(c => c.RecipientId == data.RecipientId);
                if (existing != null)
                {
                    existing.RecipientOther = data.RecipientOther;
                    existing.NameOfRecipientDepartment = data.NameOfRecipientDepartment;
                    existing.PurposeOfDataDisclosure = data.PurposeOfDataDisclosure;
                    existing.IsDataAggregatedBeforeSharing = data.IsDataAggregatedBeforeSharing;
                    existing.IsDataAnonymizedBeforeSharing = data.IsDataAnonymizedBeforeSharing;
                    existing.PurposeOfDataAccess = data.PurposeOfDataAccess;

                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDisclosureRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataDisclosureRecipient data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.DeleteAsync($"v1/DataProcessingComponents/disclosurerecipients/{data.AssessmentId}/{data.RecipientId}");


                if (Assessment.DisclosureRecipients == null)
                    Assessment.DisclosureRecipients = new List<DataProcessingDataDisclosureRecipient>();

                var disclosurerecipients = Assessment.DisclosureRecipients
                    .FirstOrDefault(c => c.AssessmentId == data.AssessmentId && c.RecipientId == data.RecipientId);
                if (disclosurerecipients != null)
                {
                    Assessment.DisclosureRecipients.Remove(disclosurerecipients);
                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddDataSharingRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSharingRecipient data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingDataSharingRecipient, object>(
                    "v1/DataProcessingComponents/sharingrecipients", data);

                if (Assessment.SharingRecipients == null)
                    Assessment.SharingRecipients = new List<DataProcessingDataSharingRecipient>();

                Assessment.SharingRecipients.Add(data);

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateDataSharingRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSharingRecipient data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PutJsonAsync<DataProcessingDataSharingRecipient, object>(
                   $"v1/DataProcessingComponents/sharingrecipients/{data.AssessmentId}/{data.RecipientId}",
                   data);

                if (Assessment.SharingRecipients == null)
                    Assessment.SharingRecipients = new List<DataProcessingDataSharingRecipient>();


                var existing = Assessment.SharingRecipients.FirstOrDefault(c => c.RecipientId == data.RecipientId);
                if (existing != null)
                {
                    existing.RecipientOther = data.RecipientOther;
                    existing.TypeOfAgreementId = data.TypeOfAgreementId;
                    existing.TypeOfAgreementOther = data.TypeOfAgreementOther;
                    existing.StatusOfAgreementId = data.StatusOfAgreementId;
                    existing.PurposeOfDataSharing = data.PurposeOfDataSharing;

                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (HttpRequestException ex)
            {
                string message = string.Empty;

                if (ex.Message.Contains("CheckConstraintError"))
                {
                    message = "If select other, specify";
                }
                else
                {
                    message = "Unable to edit data sharing recipent due to an unexpected error.";
                }

                //return (false, message);
                throw ex;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDataSharingRecipient(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataSharingRecipient data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.DeleteAsync($"v1/DataProcessingComponents/sharingrecipients/{data.AssessmentId}/{data.RecipientId}");


                if (Assessment.SharingRecipients == null)
                    Assessment.SharingRecipients = new List<DataProcessingDataSharingRecipient>();

                var Sharingrecipients = Assessment.SharingRecipients
                    .FirstOrDefault(c => c.AssessmentId == data.AssessmentId && c.RecipientId == data.RecipientId);

                if (Sharingrecipients != null)
                {
                    Assessment.SharingRecipients.Remove(Sharingrecipients);
                    await _cache.SetAsync(key, Assessment);
                }
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddDataSecurityMeasure(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSecurityMeasure data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingSecurityMeasure, object>(
                    "v1/DataProcessingComponents/securitymeasures", data);

                if (Assessment.SecurityMeasures == null)
                    Assessment.SecurityMeasures = new List<DataProcessingSecurityMeasure>();

                Assessment.SecurityMeasures.Add(data);

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateDataSecurityMeasure(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSecurityMeasure data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PutJsonAsync<DataProcessingSecurityMeasure, object>(
                    $"v1/DataProcessingComponents/securitymeasures/{data.AssessmentId}/{data.SecurityMeasureId}",
                    data);


                if (Assessment.SecurityMeasures == null)
                    Assessment.SecurityMeasures = new List<DataProcessingSecurityMeasure>();

                var existing = Assessment.SecurityMeasures.FirstOrDefault(c => c.SecurityMeasureId == data.SecurityMeasureId);
                if (existing != null)
                {
                    existing.TechnicalMeasures = data.TechnicalMeasures;
                    existing.OrganizationalMeasures = data.OrganizationalMeasures;
                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDataSecurityMeasure(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSecurityMeasure data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.DeleteAsync($"v1/DataProcessingComponents/securitymeasures/{data.AssessmentId}/{data.SecurityMeasureId}");


                if (Assessment.SecurityMeasures == null)
                    Assessment.SecurityMeasures = new List<DataProcessingSecurityMeasure>();

                var SecurityMeasures = Assessment.SecurityMeasures
                    .FirstOrDefault(c => c.AssessmentId == data.AssessmentId && c.SecurityMeasureId == data.SecurityMeasureId);

                if (SecurityMeasures != null)
                {
                    Assessment.SecurityMeasures.Remove(SecurityMeasures);
                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddSourceOfData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSourceOfData data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingSourceOfData, object>(
                    "v1/DataProcessingComponents/sourcesofdata", data);

                Assessment.SourceOfData=data;

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateSourceOfData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSourceOfData data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PutJsonAsync<DataProcessingSourceOfData, object>(
                    $"v1/DataProcessingComponents/sourcesofdata/{data.AssessmentId}",
                    data);

                // Update only the SourceOfData in cached Assessment
                var existing = Assessment.SourceOfData;
                if (existing != null )
                {
                    existing.PrimarySourceId = data.PrimarySourceId;
                    existing.PrimarySourceOther = data.PrimarySourceOther;
                    existing.SecondarySourceId = data.SecondarySourceId;
                    existing.SecondarySourceOther = data.SecondarySourceOther;

                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteSourceOfData(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingSourceOfData data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.DeleteAsync($"v1/DataProcessingComponents/sourcesofdata/{data.AssessmentId}");

                var sourceOfData = Assessment.SourceOfData;
                if (sourceOfData.AssessmentId == data.AssessmentId)
                {
                    Assessment.SourceOfData = null;
                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddDataOutput(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataOutput data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DataProcessingDataOutput, object>(
                    "v1/DataProcessingComponents/dataoutputs", data);

                if (Assessment.DataOutputs == null)
                    Assessment.DataOutputs = new List<DataProcessingDataOutput>();

                Assessment.DataOutputs.Add ( data);

                await _cache.SetAsync(key, Assessment);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateDataOutput(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataOutput data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PutJsonAsync<DataProcessingDataOutput, object>(
                    $"v1/DataProcessingComponents/dataoutputs/{data.AssessmentId}/{data.DataOutputId}",
                    data);

                var existing = Assessment.DataOutputs.FirstOrDefault(c => c.DataOutputId == data.DataOutputId);
                if (existing != null)
                {
                    existing.RecipientId = data.RecipientId;
                    existing.RecipientOfOutput = data.RecipientOfOutput;
                    existing.DataOutputAndUsageJustification = data.DataOutputAndUsageJustification;

                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDataOutput(RiskBenefitAssessmentViewModel Assessment, string Identifier, DataProcessingDataOutput data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.DeleteAsync($"v1/DataProcessingComponents/dataoutputs/{data.AssessmentId}/{data.DataOutputId}");
                
                var DataOutputs = Assessment.DataOutputs
                     .FirstOrDefault(c => c.AssessmentId == data.AssessmentId && c.DataOutputId == data.DataOutputId);
                if (DataOutputs != null)
                {
                    Assessment.DataOutputs.Remove(DataOutputs);
                    await _cache.SetAsync(key, Assessment);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateRbaStatus(RiskBenefitAssessmentViewModel Assessment, string Identifier, StatusChangeDto data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<StatusChangeDto, object>(
                   $"v1/DataProcessingComponents/update_rba_status", data);

                await _cache.RemoveAsync(key);

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<(bool, string)> SaveCoMDecision(string Identifier, DecisionDto data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DecisionDto, object>(
                   $"v1/CoMDecisions", data);

                await _cache.RemoveAsync(key);

                if (!data.DecisionStatusId.HasValue)
                    return (false, "Draft note saved. Decision is still pending.");

                return (true,"");

            }
            catch (Exception ex)
            {
                if(data.IsSubmitted)
                    return (false, "Unable to submit the Draft decision—please try again");

                return (false, "Unable to save the Draft decision—please try again");
            }
        }


        public async Task<Decision?> GetCurrentCoMDecision(string Identifier, int assessmentId)
        {
            try
            {

                Decision? coMDecision = await _api.GetAsync<Decision>($"v1/CoMDecisions/current/{assessmentId}");

                return coMDecision;

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The input does not contain any JSON tokens", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                throw ex;
            }

        }

        public async Task<(bool, string)> SaveRecommendation(string Identifier, RecommendationDto data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<RecommendationDto, object>(
                   $"v1/ROHQLegReviews", data);

                await _cache.RemoveAsync(key);

                return (true,"");

            }
            catch (Exception ex)
            {
                if (data.IsSubmitted)
                    return (false, "Unable to submit the Draft recommendation—please try again");

                return (false, "Unable to save the Draft recommendation—please try again");
            }
        }

        public async Task<Recommendation?> GetCurrentRecommendation(string Identifier, bool isleg, int assessmentId)
        {
            try
            {

                Recommendation? recommendation = await _api.GetAsync<Recommendation>($"v1/ROHQLegReviews/current/{assessmentId}/{isleg}");

                return recommendation;

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The input does not contain any JSON tokens", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                throw ex;
            }
        }

        public async Task<Decision?> GetCurrentPmDecision(string Identifier, int assessmentId)
        {
            try
            {

                Decision? pmDecison = await _api.GetAsync<Decision>($"v1/PmDecisions/current/{assessmentId}");

                return pmDecison;

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The input does not contain any JSON tokens", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                throw ex;
            }
        }

        public async Task<(bool, string)> SavePmDecision(string Identifier, DecisionDto data)
        {
            var key = GetKey(Identifier, data.AssessmentId);

            try
            {

                await _api.PostJsonAsync<DecisionDto, object>(
                   $"v1/PmDecisions", data);

                await _cache.RemoveAsync(key);

                if (!data.DecisionStatusId.HasValue)
                    return (false, "Draft note saved. Decision is still pending.");

                return (true, "");

            }
            catch (Exception ex)
            {
                if (data.IsSubmitted)
                    return (false, "Unable to submit the Draft decision—please try again");

                return (false, "Unable to save the Draft decision—please try again");
            }
        }
    }
}
