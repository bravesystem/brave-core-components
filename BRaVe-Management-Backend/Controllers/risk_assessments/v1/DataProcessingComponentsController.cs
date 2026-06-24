using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers.risk_assessments.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class DataProcessingComponentsController : ControllerBase
    {

        private readonly IAssessmentComponentService _service;

        public DataProcessingComponentsController (IAssessmentComponentService service)
        {
            _service = service;
        }


        //*************************************ADD DATA**********************************************************


        [HttpPost("update_rba_status")]
        public async Task<ActionResult> UpdateRBAStatus(StatusChangeDto data)
        {

            var userId = User.Identifier();
            var tenantId = User.Tenant();
            await _service.UpdateRBAStatus(data, tenantId, userId);

            return NoContent();
        }

        [HttpPost("personaldata")]
        public async Task<ActionResult> AddPersonalData([FromBody] DataProcessingPersonalData data)

        {

            var userId = User.Identifier();
            var tenantId = User.Tenant();
            await _service.AddPersonalData(data,tenantId,userId);

            return NoContent();
        }


        [HttpPost("dataprocessing")]
        public async Task<ActionResult> AddDataProcessing([FromBody] DataProcessing data)

        {

            var userId = User.Identifier();
            var tenantId = User.Tenant();
            await _service.AddDataProcessing(data, tenantId, userId);

            return NoContent();
        }


        [HttpPost("lawfulbasis")]
        public async Task<ActionResult> AddLawfulBasis([FromBody] DataProcessingLawfulBasis data)

        {

            var userId = User.Identifier();
            var tenantId = User.Tenant();
            await _service.AddLawfulBasis(data, tenantId, userId);

            return NoContent();
        }

        [HttpPost("dataretention")]
        public async Task<ActionResult> AddDataRetention([FromBody] DataProcessingRetention data)

        {

            var userId = User.Identifier();
            var tenantId = User.Tenant();
            await _service.AddDataRetention(data, tenantId, userId);

            return NoContent();
        }

        [HttpPost("collaborators")]
        public async Task<ActionResult> AddCollaborator([FromBody] DataProcessingCollaborator data)
        {
            var userId = User.Identifier();
            var tenantId = User.Tenant();
            //var userId = "System";
            await _service.AddCollaborator(data,tenantId, userId);
            return NoContent();
        }


        [HttpPost("datasubjects")]
        public async Task<ActionResult> AddDataSubjects([FromBody] DataProcessingDataSubject data)

        {
     
            var userId = User.Identifier();
            var tenantId = User.Tenant();
            await _service.AddDataSubjects(data,tenantId, userId);

            return NoContent();
        }

        [HttpPost("disclosurerecipients")]

        public async Task<ActionResult> AddDataDisclosureRecipients([FromBody] DataProcessingDataDisclosureRecipient data)
        {
            var userId = User.Identifier();
            var tenantId = User.Tenant();
            await _service.AddDataDisclosureRecipients(data,tenantId, userId);

            return NoContent();
        }

        [HttpPost("sharingrecipients")]

        public async Task<ActionResult> AddDataSharingRecipients([FromBody] DataProcessingDataSharingRecipient data)
        {
            var userId = User.Identifier();
            var tenantId = User.Tenant();
            await _service.AddDataSharingRecipients(data,tenantId, userId);

            return NoContent();
        }

        [HttpPost("securitymeasures")]

        public async Task<ActionResult> AddSecurityMeasures([FromBody] DataProcessingSecurityMeasure data)
        {
            var userId = User.Identifier();
            var tenantId = User.Tenant();
            //var userId = "System";
            await _service.AddSecurityMeasures(data,tenantId, userId);

            return NoContent();
        }

        [HttpPost("sourcesofdata")]

        public async Task<ActionResult> AddSourcesOfData([FromBody] DataProcessingSourceOfData data)
        {
            var userId = User.Identifier();
            var tenantId = User.Tenant();
            //var userId = "System";
            await _service.AddSourcesOfData(data,tenantId, userId);

            return NoContent();
        }

        [HttpPost("dataoutputs")]

        public async Task<ActionResult> AddDataOutputs([FromBody] DataProcessingDataOutput data)
        {
            var userId = User.Identifier();
            var tenantId = User.Tenant();
            await _service.AddDataOutputs(data,tenantId, userId);

            return NoContent();
        }


        //*****************************************************DELETE DATA*****************************************


        [HttpDelete("collaborators/{assessmentId}/{partnerId}")]
        public async Task<ActionResult> DeleteCollaborator(int assessmentId, int partnerId)
        {
            var userId = User.Identifier();
            await _service.DeleteCollaborator(assessmentId, partnerId, userId);
            return NoContent();
        }

        [HttpDelete("datasubjects/{assessmentId}/{subjectId}")]
        public async Task<ActionResult> DeleteDataSubject(int assessmentId, int subjectId)
        {
            var userId = User.Identifier();
            await _service.DeleteDataSubjects(assessmentId, subjectId, userId);
            return NoContent();
        }

        [HttpDelete("personaldata/{assessmentId}/{personalcategoryId}")]
        public async Task<ActionResult> DeletePersonalData(int assessmentId, int personalcategoryId)
        {
            var userId = User.Identifier();
            await _service.DeletePersonalData(assessmentId, personalcategoryId, userId);
            return NoContent();
        }

        [HttpDelete("securitymeasures/{assessmentId}/{measureId}")]
        public async Task<ActionResult> DeleteSecurityMeasures(int assessmentId, int measureId)
        {
            var userId = User.Identifier();
            await _service.DeleteSecurityMeasures(assessmentId, measureId, userId);
            return NoContent();
        }

        [HttpDelete("disclosurerecipients/{assessmentId}/{recipientId}")]
        public async Task<ActionResult> DeleteDisclosureRecipients(int assessmentId, int recipientId)
        {
            var userId = User.Identifier();
            await _service.DeleteDisclosureRecipients(assessmentId, recipientId, userId);
            return NoContent();
        }

        [HttpDelete("sharingrecipients/{assessmentId}/{recipientId}")]
        public async Task<ActionResult> DeleteSharingRecipients(int assessmentId, int recipientId)
        {
            var userId = User.Identifier();
            await _service.DeleteSharingRecipients(assessmentId, recipientId, userId);
            return NoContent();
        }

        //[HttpDelete("sourcesofdata/{assessmentId}/{primaryId}")]
        [HttpDelete("sourcesofdata/{assessmentId}")]
        public async Task<ActionResult> DeleteSourcesOfData(int assessmentId)
        {
            var userId = User.Identifier();
            await _service.DeleteSourcesOfData(assessmentId, userId);
            return NoContent();
        }

        [HttpDelete("dataoutputs/{assessmentId}/{outputId}")]
        public async Task<ActionResult> DeleteDataOutputs(int assessmentId, int outputId)
        {
            var userId = User.Identifier();
            await _service.DeleteDataOutputs(assessmentId, outputId, userId);
            return NoContent();
        }




        //*****************************************************EDIT DATA*****************************************


        [HttpPut("collaborators/{assessmentId}/{partnerId}")]
        public async Task<ActionResult> EditCollaborator(DataProcessingCollaborator data, int assessmentId, int partnerId)
        {
            var userId = User.Identifier();

            DataProcessingCollaborator collaborator = new DataProcessingCollaborator
            {

                AssessmentId = assessmentId,
                PartnerId = partnerId,
                PartnerOrganizationName=data.PartnerOrganizationName,
                PartnerOrganizationContacts=data.PartnerOrganizationContacts,
                Description=data.Description,
                FilePath=data.FilePath

            
            };

            await _service.EditCollaborator(collaborator, userId);
            return NoContent();
        }

        [HttpPut("datasubjects/{assessmentId}/{subjectId}")]
        public async Task<ActionResult> EditDataSubjects(DataProcessingDataSubject data, int assessmentId, int subjectId)
        {
            var userId = User.Identifier();

            DataProcessingDataSubject subject = new DataProcessingDataSubject
            {

                AssessmentId = assessmentId,
                SubjectTypeId = subjectId,
                IsVulnerableGroup = data.IsVulnerableGroup,
 
            };

            await _service.EditDataSubjects(subject, userId);
            return NoContent();
        }

        [HttpPut("personaldata/{assessmentId}/{personalcategoryId}")]
        public async Task<ActionResult> EditPersonalData(DataProcessingPersonalData data, int assessmentId, int personalcategoryId)
        {
            var userId = User.Identifier();

            DataProcessingPersonalData PersonalData = new DataProcessingPersonalData
            {

                AssessmentId = assessmentId,
                PersonalDataCategoryId = personalcategoryId,
                IsSpecialCategory = data.IsSpecialCategory,
                PurposeOfDataCollection = data.PurposeOfDataCollection,

            };
            await _service.EditPersonalData(PersonalData, userId);
            return NoContent();
        }

        [HttpPut("securitymeasures/{assessmentId}/{measureId}")]
        public async Task<ActionResult> EditSecurityMeasures(DataProcessingSecurityMeasure data, int assessmentId, int measureId)
        {
            var userId = User.Identifier();

            DataProcessingSecurityMeasure SecurityMeasure = new DataProcessingSecurityMeasure
            {

                AssessmentId = assessmentId,
                SecurityMeasureId = measureId,
                TechnicalMeasures = data.TechnicalMeasures,
                OrganizationalMeasures = data.OrganizationalMeasures,

            };

            await _service.EditSecurityMeasures(SecurityMeasure, userId);
            return NoContent();
        }

        [HttpPut("disclosurerecipients/{assessmentId}/{recipientId}")]
        public async Task<ActionResult> EditDisclosureRecipient(DataProcessingDataDisclosureRecipient data, int assessmentId, int recipientId)
        {
            var userId = User.Identifier();

            DataProcessingDataDisclosureRecipient disclosure = new DataProcessingDataDisclosureRecipient
            {

                AssessmentId = assessmentId,
                RecipientId = recipientId,
                RecipientOther = data.RecipientOther,
                NameOfRecipientDepartment = data.NameOfRecipientDepartment,
                PurposeOfDataDisclosure = data.PurposeOfDataDisclosure,
                IsDataAggregatedBeforeSharing = data.IsDataAggregatedBeforeSharing,
                IsDataAnonymizedBeforeSharing = data.IsDataAnonymizedBeforeSharing,
                PurposeOfDataAccess = data.PurposeOfDataAccess,


            };

            await _service.EditDisclosureRecipients(disclosure, userId);
            return NoContent();
        }

        [HttpPut("sharingrecipients/{assessmentId}/{recipientId}")]
        public async Task<ActionResult> EditSharingRecipient(DataProcessingDataSharingRecipient data, int assessmentId, int recipientId)
        {
            var userId = User.Identifier();

            DataProcessingDataSharingRecipient sharingrecipient = new DataProcessingDataSharingRecipient
            {

                AssessmentId = assessmentId,
                RecipientId = recipientId,
                RecipientOther = data.RecipientOther,
                TypeOfAgreementId = data.TypeOfAgreementId,
                TypeOfAgreementOther = data.TypeOfAgreementOther,
                StatusOfAgreementId = data.StatusOfAgreementId,
                PurposeOfDataSharing = data.PurposeOfDataSharing,


            };

            await _service.EditSharingRecipients(sharingrecipient, userId);
            return NoContent();
        }

        [HttpPut("sourcesofdata/{assessmentId}")]
        public async Task<ActionResult> EditSourcesOfData(DataProcessingSourceOfData data, int assessmentId)
        {
            var userId = User.Identifier();

            DataProcessingSourceOfData source = new DataProcessingSourceOfData
            {

                AssessmentId = assessmentId,
                PrimarySourceId = data.PrimarySourceId,
                PrimarySourceOther = data.PrimarySourceOther,
                SecondarySourceId = data.SecondarySourceId,
                SecondarySourceOther = data.SecondarySourceOther,
                AttachmentUrl = data.AttachmentUrl,

            };

            await _service.EditSourcesOfData(source, userId);
            return NoContent();
        }

        [HttpPut("dataoutputs/{assessmentId}/{outputId}")]
        public async Task<ActionResult> EditDataOutputs(DataProcessingDataOutput data, int assessmentId, int outputId)
        {
            var userId = User.Identifier();

            DataProcessingDataOutput output = new DataProcessingDataOutput
            {

                AssessmentId = assessmentId,
                DataOutputId = outputId,
                RecipientId = data.RecipientId,
                RecipientOfOutput = data.RecipientOfOutput,
                DataOutputAndUsageJustification = data.DataOutputAndUsageJustification,


            };

            await _service.EditDataOutputs(output, userId);
            return NoContent();
        }


    }
}
