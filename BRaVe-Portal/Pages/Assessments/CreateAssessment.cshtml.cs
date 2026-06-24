using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.Enums;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BRaVe_Portal.Pages.Assessment
{
    public class CreateAssessmentModel : PageModel
    {

        private readonly ILogger<CreateAssessmentModel> _logger;
        private readonly IAppCache _cache;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public CreateAssessmentModel(ILogger<CreateAssessmentModel> logger, IAppCache cache, IRestApiService api, ILookupService lookupService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

        public RiskBenefitAssessmentViewModel Assessment { get; set; } = new();

        [BindProperty] public int AssessmentId { get; set; }

        [BindProperty] public int SelectedCollaboratorId { get; set; }
        [BindProperty] public int SelectedSubjectId { get; set; }
        [BindProperty] public int SelectedPersonalDataId { get; set; }
        [BindProperty] public int SelectedDisclosureRecipientId { get; set; }
        [BindProperty] public int SelectedSharingRecipientId { get; set; }
        [BindProperty] public int SelectedSecurityMeasureId { get; set; }
        [BindProperty] public int SelectedDataOutputId { get; set; }

        // Lookup options for selects
        public List<SelectListItem> CollaboratorOptions { get; set; } = new();
        public List<SelectListItem> SubjectOptions { get; set; } = new();
        public List<SelectListItem> PersonalDataOptions { get; set; } = new();
        public List<SelectListItem> DisclosureRecipientOptions { get; set; } = new();
        public List<SelectListItem> SharingRecipientOptions { get; set; } = new();
        public List<SelectListItem> SecurityMeasureOptions { get; set; } = new();
        public List<SelectListItem> DataOutputOptions { get; set; } = new();

        public string GetKey(string identifier, int assessment)
        {
            return $"rba:{identifier}:{assessment}";
        }

        public async Task<IActionResult> OnGet(int assessmentId)
        {
            _logger.LogInformation("OnGet called for AssessmentId={AssessmentId}", assessmentId);

            int tenant = User.Tenant();

            AssessmentId = assessmentId;

            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

            ViewData["CoreLookups"] = coreLookups;

            RiskBenefitAssessmentDto AssessmentDto = new RiskBenefitAssessmentDto();

            var key = GetKey(User.Identifier(), assessmentId);

            if (TempData["NewAssessment"] != null)
            {
                AssessmentDto = TempData.Get<RiskBenefitAssessmentDto>("NewAssessment");

                Assessment.AssessmentId = AssessmentDto.AssessmentId;
                Assessment.TenantId = AssessmentDto.TenantId;
                Assessment.RequireApprovals = AssessmentDto.RequireApprovals;
                Assessment.StatusId = AssessmentDto.StatusId; //AssessmentDto.StatusId;
                Assessment.CreatedByUserId = AssessmentDto.CreatedByUserId;
                Assessment.CreatedOn = AssessmentDto.CreatedOn;

                _cache.SetAsync(key, Assessment);

                _logger.LogInformation("Loaded assessment from TempData for AssessmentId={AssessmentId}", assessmentId);
            }
            else if (await _cache.ExistsAsync(key))
            {
                Assessment = await _cache.GetAsync<RiskBenefitAssessmentViewModel>(key);
                _logger.LogInformation("Loaded assessment from cache for AssessmentId={AssessmentId}", assessmentId);
            }
            else
            {
                TempData["Error"] = "Assessment not found—please try again";
                _logger.LogWarning("Assessment not found in TempData or cache for AssessmentId={AssessmentId}", assessmentId);
                return RedirectToPage("Index");
            }

            return Page();
        }

        // ================= SAVE FULL ASSESSMENT =================
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveAssessment(int assessmentId)
        {
            _logger.LogInformation("OnPostSaveAssessment called for AssessmentId={AssessmentId}", assessmentId);

            AssessmentId = assessmentId;

            if (assessmentId == 0) //keep this for now.
            {
                TempData["Error"] = "Assessment not found—please try again";
                _logger.LogWarning("Invalid AssessmentId=0 in OnPostSaveAssessment");
                return RedirectToPage("Index");
            }

            var key = GetKey(User.Identifier(), AssessmentId);

            if (await _cache.ExistsAsync(key))
            {
                Assessment = await _cache.GetAsync<RiskBenefitAssessmentViewModel>(key);
            }
            else
            {
                TempData["Error"] = "Assessment not found—please try again";
                _logger.LogWarning("Assessment key not found in cache for AssessmentId={AssessmentId}", assessmentId);
                return RedirectToPage("Index");
            }

            TempData["SuccessMessage"] = "Assessment saved successfully!";
            _logger.LogInformation("Assessment saved successfully for AssessmentId={AssessmentId}", assessmentId);

            await _cache.RemoveAsync(key);

            return RedirectToPage("Index");
        }

        // ================= ADD MODALS =================
        [ValidateAntiForgeryToken]
        public IActionResult OnPostAddCollaborator(DataProcessingCollaborator collaborator)
        {
            _logger.LogInformation("OnPostAddCollaborator called");

            if (ModelState.IsValid)
            {
                // TODO: Save to DB
                TempData["SuccessMessage"] = "Collaborator added.";
                _logger.LogInformation("Collaborator added successfully");
            }
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostAddDataSubject(DataProcessingDataSubject subject)
        {
            _logger.LogInformation("OnPostAddDataSubject called");

            if (ModelState.IsValid)
            {
                // TODO: Save to DB
                TempData["SuccessMessage"] = "Data Subject added.";
                _logger.LogInformation("Data Subject added successfully");
            }
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddPersonalData(DataProcessingPersonalData personalData)
        {
            _logger.LogInformation("OnPostAddPersonalData called for AssessmentId={AssessmentId}", personalData.AssessmentId);

            var key = GetKey(User.Identifier(), personalData.AssessmentId);

            if (await _cache.ExistsAsync(key))
            {
                Assessment = await _cache.GetAsync<RiskBenefitAssessmentViewModel>(key);
            }
            else
            {
                _logger.LogWarning("Assessment key not found in cache for AssessmentId={AssessmentId}", personalData.AssessmentId);
                return RedirectToPage("Index");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Unable to add personal data category—please try again";
                _logger.LogWarning("ModelState invalid in OnPostAddPersonalData for AssessmentId={AssessmentId}", personalData.AssessmentId);
                return Page();
            }

            TempData["SuccessMessage"] = "Personal Data Category added.";
            _logger.LogInformation("Personal Data Category added for AssessmentId={AssessmentId}", personalData.AssessmentId);

            Assessment.PersonalData.Add(personalData);

            _cache.SetAsync(key, Assessment);

            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            ViewData["CoreLookups"] = await _lookupService.GetCoreLookups(languageCode);

            return Page();
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostAddDisclosureRecipient(DataProcessingDataDisclosureRecipient disclosureRecipient)
        {
            _logger.LogInformation("OnPostAddDisclosureRecipient called");

            if (ModelState.IsValid)
            {
                // TODO: Save to DB
                TempData["SuccessMessage"] = "Disclosure Recipient added.";
                _logger.LogInformation("Disclosure Recipient added successfully");
            }
            return RedirectToPage();
        }

        public IActionResult OnPostAddSharingRecipient(DataProcessingDataSharingRecipient sharingRecipient)
        {
            _logger.LogInformation("OnPostAddSharingRecipient called");

            if (ModelState.IsValid)
            {
                // TODO: Save to DB
                TempData["SuccessMessage"] = "Sharing Recipient added.";
                _logger.LogInformation("Sharing Recipient added successfully");
            }
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostAddSecurityMeasure(DataProcessingSecurityMeasure securityMeasure)
        {
            _logger.LogInformation("OnPostAddSecurityMeasure called");

            if (ModelState.IsValid)
            {
                // TODO: Save to DB
                TempData["SuccessMessage"] = "Security Measure added.";
                _logger.LogInformation("Security Measure added successfully");
            }
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostAddDataOutput(DataProcessingDataOutput dataOutput)
        {
            _logger.LogInformation("OnPostAddDataOutput called");

            if (ModelState.IsValid)
            {
                // TODO: Save to DB
                TempData["SuccessMessage"] = "Data Output added.";
                _logger.LogInformation("Data Output added successfully");
            }
            return RedirectToPage();
        }
    }
}
