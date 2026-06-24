using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.Enums;
using BRaVe_Portal.Models.ViewModels;
using BRaVe_Portal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Net;
using static BRaVe_Portal.Helpers.KeyVaultSecretNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Portal.Pages.Assessments
{
    public class PMUpdateAssessmentModel : PageModel
    {

        private readonly ILogger<IndexModel> _logger;

        private readonly ILookupService _lookupService;

        private readonly IRBAHelperService _rBAHelperService;

        //private readonly string languageCode;

        private readonly IBlobStorageService _blobStorage;

        private const string RBA_ATTACCHMENTS = "rba-attachments";


        public PMUpdateAssessmentModel(ILogger<IndexModel> logger, ILookupService lookupService, IRBAHelperService rBAHelperService,IBlobStorageService blobStorage)
        {
            _logger = logger;
            _lookupService = lookupService;
            _rBAHelperService = rBAHelperService;
            _blobStorage = blobStorage;

            //languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }


        public RiskBenefitAssessmentViewModel Assessment { get; set; } = new();


        public string ProgramManagerName { get; set; }

        public string ProgramManagerContacts { get; set; }

        [BindProperty]
        public int AssessmentId { get; set; }

        [BindProperty]
        public IFormFile? SourceFile { get; set; }

        public IFormFile? CollaboratorFile { get; set; }

        public IFormFile? LawfulBasisDocPath { get; set; }

        public IFormFile? InitialDataCollectionDocPath { get; set; }
        
        public IFormFile? OngoingDataManagementDocPath { get; set; }

        public IFormFile? DataSharingDocPath { get; set; }


        //[BindProperty]
        //public bool SectionE { get; set; }

        public async Task<IActionResult> OnGet(int assessmentId, AssessmentStatus statusId)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

            ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
            ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;


            AssessmentId = assessmentId;
            ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

            ViewData["CoreLookups"] = coreLookups;

            return Page();

        }


        //ADD SECTION A
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveSectionA(DataProcessing data)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), data.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "SectionA";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to complete section A用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                data.ProgamManagerName = "";
                data.ProgamManagerContacts = "";

                //ModelState.Clear();

                if (await _rBAHelperService.UpdateDataProcessing(Assessment, User.Identifier(), data))
                {

                    TempData["SuccessMessage"] = "Data Processing information added successfully";

                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else 
                {
                    TempData["ErrorMessage"] = "Unable to add Data processing information. Make sure the primary purpose is different from the secondary purpose.";
                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch(Exception ex)
            {
                return RedirectToPage("Index");

            }



        }


        //ADD SECTION E
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveSectionE(DataProcessingLawfulBasis lawdata)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), lawdata.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "SectionE";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to complete section E用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();

                // Lawful Basis
                if (LawfulBasisDocPath != null && LawfulBasisDocPath.Length > 0)
                {
                    await using var stream = LawfulBasisDocPath.OpenReadStream();

                    string filename =
                        $"Tenant-{User.Tenant()}/{AssessmentId}/{Guid.NewGuid()}_{LawfulBasisDocPath.FileName}";

                    var blobUrl = await _blobStorage.WriteAsync(
                        containerName: RBA_ATTACCHMENTS,
                        blobPath: filename,
                        content: stream,
                        contentType: LawfulBasisDocPath.ContentType
                    );

                    lawdata.LawfulBasisDocPath = blobUrl;
                }

                // Initial Data Collection
                if (InitialDataCollectionDocPath != null && InitialDataCollectionDocPath.Length > 0)
                {
                    await using var stream = InitialDataCollectionDocPath.OpenReadStream();

                    string filename =
                        $"Tenant-{User.Tenant()}/{AssessmentId}/{Guid.NewGuid()}_{InitialDataCollectionDocPath.FileName}";

                    var blobUrl = await _blobStorage.WriteAsync(
                        containerName: RBA_ATTACCHMENTS,
                        blobPath: filename,
                        content: stream,
                        contentType: InitialDataCollectionDocPath.ContentType
                    );

                    lawdata.InitialDataCollectionDocPath = blobUrl;
                }

                // Ongoing Data Management
                if (OngoingDataManagementDocPath != null && OngoingDataManagementDocPath.Length > 0)
                {
                    await using var stream = OngoingDataManagementDocPath.OpenReadStream();

                    string filename =
                        $"Tenant-{User.Tenant()}/{AssessmentId}/{Guid.NewGuid()}_{OngoingDataManagementDocPath.FileName}";

                    var blobUrl = await _blobStorage.WriteAsync(
                        containerName: RBA_ATTACCHMENTS,
                        blobPath: filename,
                        content: stream,
                        contentType: OngoingDataManagementDocPath.ContentType
                    );

                    lawdata.OngoingDataManagementDocPath = blobUrl;
                }

                // Data Sharing
                if (DataSharingDocPath != null && DataSharingDocPath.Length > 0)
                {
                    await using var stream = DataSharingDocPath.OpenReadStream();

                    string filename =
                        $"Tenant-{User.Tenant()}/{AssessmentId}/{Guid.NewGuid()}_{DataSharingDocPath.FileName}";

                    var blobUrl = await _blobStorage.WriteAsync(
                        containerName: RBA_ATTACCHMENTS,
                        blobPath: filename,
                        content: stream,
                        contentType: DataSharingDocPath.ContentType
                    );

                    lawdata.DataSharingDocPath = blobUrl;
                }


                if (await _rBAHelperService.UpdateLawBases(Assessment, User.Identifier(), lawdata))
                {

                    TempData["SuccessMessage"] = "Lawful bases information added successfully";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to add Lawful bases information用lease try again";
                    
                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }
          
        }

        //ADD SECTION H
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveSectionH(DataProcessingRetention retdata)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), retdata.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "SectionH";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to complete section H用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

               // ModelState.Clear();

                if (await _rBAHelperService.UpdateRetention(Assessment, User.Identifier(), retdata))
                {

                    TempData["SuccessMessage"] = "Data retention information added successfully";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to add data retention information用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("/Programs/Index");

            }


        }


        //ADD PERSONAL DATA
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddPersonalData(DataProcessingPersonalData personalData)
        {

            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), personalData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "PersonalData";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to complete section H用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();

                if (await _rBAHelperService.AddPersonalData(Assessment, User.Identifier(), personalData))
                {

                    TempData["SuccessMessage"] = "Personal Data was added successfully";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to add personal data category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }
           
        }

        //EDIT PERSONAL DATA
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditPersonalData(DataProcessingPersonalData personalData)
        {

            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), personalData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "PersonalData";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to complete section H用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();

                if (await _rBAHelperService.UpdatePersonalData(Assessment, User.Identifier(), personalData))
                {

                    TempData["SuccessMessage"] = "Personal Data was updated successfully";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to update personal data category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }
            
        }



        //DELETE PersonalData
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeletePersonalData(int assessmentId, int Id)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;


            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "PersonalData";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to complete section H用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                ModelState.Clear();

                if (await _rBAHelperService.DeletePersonalData(Assessment, User.Identifier(), new DataProcessingPersonalData { 
                    AssessmentId = assessmentId,
                    PersonalDataCategoryId = Id
                }))
                {

                    TempData["SuccessMessage"] = "Personal Data was removed successfully.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to remove personal data due to an unexpected error.";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }

         
        }


        //ADD COLLABORATOR
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddCollaborator(DataProcessingCollaborator collaboratorData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {

                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), collaboratorData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "Collaborator";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to add collaborator data category用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();

                // Handle attachment upload to Azure
                if (CollaboratorFile != null && CollaboratorFile.Length > 0)
                {
                    await using var stream = CollaboratorFile.OpenReadStream();

                    string filename = $"Tenant-{User.Tenant()}/{AssessmentId}/{Guid.NewGuid()}_{CollaboratorFile.FileName}";

                    var blobUrl = await _blobStorage.WriteAsync(
                        containerName: RBA_ATTACCHMENTS,
                        blobPath: filename,
                        content: stream,
                        contentType: CollaboratorFile.ContentType
                    );

                    // Store only the URL 
                    collaboratorData.FilePath = blobUrl;
                }

                (bool success, string message) = await _rBAHelperService.AddDataCollaborator(Assessment, User.Identifier(), collaboratorData);

                if(success)
                {

                    TempData["SuccessMessage"] = "Collaborator data category was added successfully";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = message;


                }

                return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }



        }

        //EDIT COLLABORATOR
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> OnPostEditCollaborator(DataProcessingCollaborator collaboratorData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), collaboratorData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "Collaborator";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to update collaborator用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();


                // Handle attachment upload to Azure
                if (CollaboratorFile != null && CollaboratorFile.Length > 0)
                {
                    await using var stream = CollaboratorFile.OpenReadStream();

                    string filename = $"Tenant-{User.Tenant()}/{AssessmentId}/{Guid.NewGuid()}_{CollaboratorFile.FileName}";

                    var blobUrl = await _blobStorage.WriteAsync(
                        containerName: RBA_ATTACCHMENTS,
                        blobPath: filename,
                        content: stream,
                        contentType: CollaboratorFile.ContentType
                    );

                    // Store only the URL 
                    collaboratorData.FilePath = blobUrl;
                }

                if (await _rBAHelperService.UpdateDataCollaborator(Assessment, User.Identifier(), collaboratorData))
                {

                    TempData["SuccessMessage"] = "Collaborator was updated successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to update collaborator用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }


        }


        //DELETE COLLABORATOR

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> OnPostDeleteCollaborator(int assessmentId, int Id)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "Collaborator";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to delete collaborator due to an unexpected error用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                ModelState.Clear();

                if (await _rBAHelperService.DeleteDataCollaborator(Assessment, User.Identifier(), new DataProcessingCollaborator
                {
                    AssessmentId = assessmentId,
                    PartnerId = Id
                }))
                {

                    TempData["SuccessMessage"] = "Collaborator was removed successfully.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to delete collaborator due to an unexpected error.";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }


        }




        //ADD DATA SUBJECT
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddSubject(DataProcessingDataSubject subjectData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), subjectData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DataSubject";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to add collaborator data category用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

               // ModelState.Clear();

                
                if (await _rBAHelperService.AddDataSubject(Assessment, User.Identifier(), subjectData))
                {

                    TempData["SuccessMessage"] = "Data Subject was added successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to add data subject用lease try again";
               

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }


        }

        //EDIT DATASUBJECT
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditDataSubject(DataProcessingDataSubject subjectData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), subjectData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DataSubject";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to update data subject用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();

                if (await _rBAHelperService.UpdateDataSubject(Assessment, User.Identifier(), subjectData))
                {

                    TempData["SuccessMessage"] = "Data subject was updated successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to update data subject due to an unexpected error.";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }

        }

        //DELETE DATA SUBJECT
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteDataSubject(int assessmentId, int Id)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DataSubject";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to delete data subject用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                ModelState.Clear();

                if (await _rBAHelperService.DeleteDataSubject(Assessment, User.Identifier(), new DataProcessingDataSubject
                {
                    AssessmentId = assessmentId,
                    SubjectTypeId = Id
                }))
                {

                    TempData["SuccessMessage"] = "Data subject was removed successfully.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to remove data subject due to an unexpected error.";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }

        }



        //ADD DATA DISCLOSURE RECEPIENT
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> OnPostAddDisclosureRecipient(DataProcessingDataDisclosureRecipient recipientData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), recipientData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DisclosureRecipient";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to add disclosure recipient data category用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();


                if (await _rBAHelperService.AddDisclosureRecipient(Assessment, User.Identifier(), recipientData))
                {

                    TempData["SuccessMessage"] = "Disclosure recipient was added successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to add disclosure recipient data category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }


        }

        //EDIT DATA DISCLOSURE RECEPIENT

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditDisclosureRecipient(DataProcessingDataDisclosureRecipient recipientData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), recipientData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DisclosureRecipient";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to edit disclosure recipient data category用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();


                if (await _rBAHelperService.UpdateDisclosureRecipient(Assessment, User.Identifier(), recipientData))
                {

                    TempData["SuccessMessage"] = "Disclosure recipient was updated successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to update disclosure recipient data category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }
     
        }



        //DELETE  DATA DISCLOSURE RECEPIENT
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteDisclosureRecipient(int assessmentId, int Id)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DisclosureRecipient";


                if (await _rBAHelperService.DeleteDisclosureRecipient(Assessment, User.Identifier(), new DataProcessingDataDisclosureRecipient { 
                    AssessmentId = assessmentId,
                    RecipientId =  Id

                }))
                {

                    TempData["SuccessMessage"] = "Disclosure recipient was removed successfully.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to remove Disclosure recipient due to an unexpected error.";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }

          
        }


        //ADD DATA SHARING RECEPIENT
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddSharingRecipient(DataProcessingDataSharingRecipient sharingData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), sharingData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "SharingRecipient";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to add sharing recipient data category用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();


                if (await _rBAHelperService.AddDataSharingRecipient(Assessment, User.Identifier(), sharingData))
                {

                    TempData["SuccessMessage"] = "Sharing recipient was added successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to add sharing recipient用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }
            
        }

        //EDIT DATA SHARING RECEPIENT
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditSharingRecipient(DataProcessingDataSharingRecipient sharingData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), sharingData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "SharingRecipient";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to update data recipient用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();


                if (await _rBAHelperService.UpdateDataSharingRecipient(Assessment, User.Identifier(), sharingData))
                {

                    TempData["SuccessMessage"] = "Sharing recipient was updated successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to update sharing recipient用lease try again";

                }

                AssessmentId = Assessment.AssessmentId;

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }

        }


        //DELETE  DATA SHARING RECEPIENT
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteSharingRecipient(int assessmentId, int Id)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "SharingRecipient";


                if (await _rBAHelperService.DeleteDataSharingRecipient(Assessment, User.Identifier(), new DataProcessingDataSharingRecipient { 
                    AssessmentId = assessmentId,
                    RecipientId = Id      
                }))
                {

                    TempData["SuccessMessage"] = "Sharing recipient was removed successfully.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to remove sharing recipient用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }

        }



        //ADD SECURITY MEASURES
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddSecurityMeasure(DataProcessingSecurityMeasure securityMeasure)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), securityMeasure.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "SecurityMeasures";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to add security Measure data category用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

               // ModelState.Clear();


                if (await _rBAHelperService.AddDataSecurityMeasure(Assessment, User.Identifier(), securityMeasure))
                {

                    TempData["SuccessMessage"] = "Security measure was added successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to add security Measure data category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }


        }

        //EDIT DATA SECURITY MEASURES
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditSecurityMeasure(DataProcessingSecurityMeasure securityMeasure)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), securityMeasure.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "SecurityMeasures";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to add security Measure data category用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();


                if (await _rBAHelperService.UpdateDataSecurityMeasure(Assessment, User.Identifier(), securityMeasure))
                {

                    TempData["SuccessMessage"] = "Security measure was updated successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to update security Measure data category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }


        }


        //DELETE  DATA SECURITY MEASURES
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteSecurityMeasure(int assessmentId, int Id)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "SecurityMeasures";


                if (await _rBAHelperService.DeleteDataSecurityMeasure(Assessment, User.Identifier(), new DataProcessingSecurityMeasure { 
                    AssessmentId = assessmentId,
                    SecurityMeasureId = Id
                
                }))
                {

                    TempData["SuccessMessage"] = "Security measure was removed successfully.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to remove security Measure data category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }


        }


        //ADD SOURCES OF DATA
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddSourceOfData(DataProcessingSourceOfData SourceOfData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), SourceOfData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DataSource";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to add source of data data category用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();

                // Handle attachment upload to Azure
                if (SourceFile != null && SourceFile.Length > 0)
                {
                    await using var stream = SourceFile.OpenReadStream();

                    string filename = $"Tenant-{User.Tenant()}/{AssessmentId}/{Guid.NewGuid()}_{SourceFile.FileName}";

                    var blobUrl = await _blobStorage.WriteAsync(
                        containerName: RBA_ATTACCHMENTS,
                        blobPath: filename ,
                        content: stream,
                        contentType: SourceFile.ContentType
                    );

                    // Store only the URL 
                    SourceOfData.AttachmentUrl = blobUrl;
                }


                if (await _rBAHelperService.AddSourceOfData(Assessment, User.Identifier(), SourceOfData))
                {

                    TempData["SuccessMessage"] = "Source of data was added successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to add source of data category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }


        }


        //EDIT SOURCES OF DATA
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditSourceOfData(DataProcessingSourceOfData SourceOfData)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), SourceOfData.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DataSource";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to update source of data data category用lease try again";
                    return await OnGet(Assessment.AssessmentId, Assessment.Status);
                }

                //ModelState.Clear();

                if (SourceFile != null && SourceFile.Length > 0)
                {
                    await using var stream = SourceFile.OpenReadStream();

                    string filename = $"Tenant-{User.Tenant()}/{AssessmentId}/{Guid.NewGuid()}_{SourceFile.FileName}";

                    var blobUrl = await _blobStorage.WriteAsync(
                        containerName: RBA_ATTACCHMENTS,
                        blobPath: filename,
                        content: stream,
                        contentType: SourceFile.ContentType
                    );

                    // Store only the URL (do NOT store the file itself)
                    SourceOfData.AttachmentUrl = blobUrl;
                }


                if (await _rBAHelperService.UpdateSourceOfData(Assessment, User.Identifier(), SourceOfData))
                {

                    TempData["SuccessMessage"] = "Source of data was updated successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to update source of data category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }

        }



        //DELETE  SOURCES OF DATA
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteSourceOfData(int AssessmentId)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DataSource";

         
                if (await _rBAHelperService.DeleteSourceOfData(Assessment, User.Identifier(), new DataProcessingSourceOfData { 
                    AssessmentId = AssessmentId
                }))
                {

                    TempData["SuccessMessage"] = "Data Source was deleted successfully.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to delete DataSource due to an unexpected error用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }

        }


        //ADD DATA OUTPUTS
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddDataOutput(DataProcessingDataOutput DataOutput)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), DataOutput.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DataOutput";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to add Data Output data category用lease try again";
                    return Page();
                }

                //ModelState.Clear();


                if (await _rBAHelperService.AddDataOutput(Assessment, User.Identifier(), DataOutput))
                {

                    TempData["SuccessMessage"] = "Data output was added successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to add data output category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }
         
        }

        //EDIT DATA OUTPUTS
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditDataOutput(DataProcessingDataOutput DataOutput)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), DataOutput.AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DataOutput";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to update Data Output data category用lease try again";
                    return Page();
                }

                //ModelState.Clear();


                if (await _rBAHelperService.UpdateDataOutput(Assessment, User.Identifier(), DataOutput))
                {

                    TempData["SuccessMessage"] = "Data output was updated successfully.";
                    return RedirectToPage(new { assessmentId = Assessment.AssessmentId, statusId = Assessment.StatusId });

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to update data output category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }

        }

        //DELETE  DATA OUTPUTS
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteDataOutput(int AssessmentId, int Id)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), AssessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                TempData["Section"] = "DataOutput";

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to update Data Output data category用lease try again";
                    return Page();
                }

                ModelState.Clear();


                if (await _rBAHelperService.DeleteDataOutput(Assessment, User.Identifier(), new DataProcessingDataOutput { 
                    AssessmentId = AssessmentId,
                    DataOutputId = Id
                }))
                {

                    TempData["SuccessMessage"] = "Data output was removed successfully.";

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to remove data output category用lease try again";

                }

                return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }
            catch (Exception ex)
            {
                return RedirectToPage("Index");

            }


         
        }
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSubmitAssessment(int assessmentId)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Unable to update Data Output data category用lease try again";
                    return Page();
                }

                ModelState.Clear();

                int nextStatus = (int)AssessmentStatus.Submitted;

                if (!Assessment.RequireApprovals)
                {
                    nextStatus = (int)AssessmentStatus.Approved;    
                }

                if (await _rBAHelperService.UpdateRbaStatus(Assessment, User.Identifier(), new StatusChangeDto
                {

                    AssessmentId = AssessmentId,
                    FromStatus = Assessment.StatusId,
                    ToStatus = nextStatus

                }))
                {

                    TempData["SuccessMessage"] = "Risk benefit assessment submitted successfully.";

                    return RedirectToPage("/Programs/Index");

                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to submit Risk benefit assessment用lease try again";

                }

                //return Page();

            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to submit Risk benefit assessment用lease try again");

                //return await OnGet(Assessment.AssessmentId, Assessment.Status);

            }

            return await OnGet(Assessment.AssessmentId, Assessment.Status);


        }


        //DOWNLOAD HANDLER FOR THE MOCKED AZURE STORAGE
        public async Task<IActionResult> OnGetDownloadAttachmentAsync(string blobPath)

        {
            try
            {
                var file = await _blobStorage.ReadAsync(
                    containerName: RBA_ATTACCHMENTS,
                    blobPath: blobPath);


                var bytes = file.Content.ToArray();
                var contentType = file.Details.ContentType ?? "application/octet-stream";
                var downloadFileName = Path.GetFileName(blobPath); // e.g., "doc.pdf"


                return File(
                    bytes,
                    contentType,
                    downloadFileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Attachment not found.");
            }
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveAssessment(int assessmentId)
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);
                
                AssessmentId = Assessment.AssessmentId;

                var customValidationResult = Validate(Assessment);

                IsValid = customValidationResult.IsValid;

                TempData["SaveAssessment"] = true;

                if (!customValidationResult.IsValid)
                {
                    ModelState.AddModelError(string.Empty, "Risk benefit assessment draft saved but incomplete.");
                    ModelState.AddModelError(string.Empty, customValidationResult.Message);
                }
                else
                {
                    TempData["SuccessMessage"] = "Risk benefit assessment draft saved successfully.";
                }


            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to save Risk benefit assessment用lease try again"); 

            }


            return await OnGet(Assessment.AssessmentId, Assessment.Status);


        }


        [BindProperty]
        public bool IsValid { get; set; } = false;


        private CustomValidationResult Validate(RiskBenefitAssessmentViewModel assessment)
        {
            if (!assessment.DataProcessing.IsCaptured)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "Data Processing is not captured."
                };

            if (assessment.Collaborators.Count == 0)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "At least one Data Collaborator is required."
                };

            if (assessment.DataOutputs.Count == 0)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "At least one Data output is required."
                };

            if (assessment.DisclosureRecipients.Count == 0)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "At least one Data disclosure recipient is required."
                };

            if (assessment.SharingRecipients.Count == 0)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "At least one Data sharing recipient is required."
                };

            if (!assessment.LawfulBasis.HasData)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "Data Lawful basis information is not captured."
                };

            if (!assessment.Retention.HasData)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "Data retention information is not captured."
                };

            if (!assessment.SourceOfData.IsCaptured)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "Source of data is not captured."
                };

            if (assessment.PersonalData.Count == 0)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "At least one personal data is required."
                };

            if (assessment.Subjects.Count == 0)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "At least one Data subject is required."
                };

            if (assessment.SecurityMeasures.Count == 0)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "At least one Data security measure is required."
                };


            return  new CustomValidationResult
            {
                IsValid = true,
                Message = ""
            };

        }

        public string GetKey(string identifier, int assessment)
        {
            return $"rba:{identifier}:{assessment}";
        }

    }
}
