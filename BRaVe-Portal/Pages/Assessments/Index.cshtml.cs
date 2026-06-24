
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
using System;
using System.Globalization;

namespace BRaVe_Portal.Pages.Assessments
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly ILookupService _lookupService;

        private readonly IAppCache _cache;

        private readonly IRestApiService _api;

        private readonly string languageCode;

        public IndexModel(ILogger<IndexModel> logger, ILookupService lookupService, IAppCache cache, IRestApiService api)
        {
            _logger = logger;
            _lookupService = lookupService;
            _cache = cache;
            _api = api;

            languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }


        private static List<RiskBenefitAssessmentViewModel> _Assessments { get; set; } = new(); //my db

        private static int first = 0;

        [BindProperty]
        public List<AssessmentViewModel> Ongoing_Assessments { get; set; } = new();

        [BindProperty]
        public List<AssessmentViewModel> Decided_Assessments { get; set; } = new();


        //[BindProperty]
        public string ErrorMessage { get; set; }

        //[BindProperty]
        public string SuccessMessage { get; set; }

        public async Task<IActionResult> OnGet()
        {
            //var test = await _cache.GetAsync<string>("keep");

            if (TempData["Error"] != null)
            {
                ErrorMessage = TempData["Error"].ToString();
            }


            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"].ToString();
            }


            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

            ViewData["CoreLookups"] = coreLookups;


            List<AssessmentViewModel> assessments = new List<AssessmentViewModel>();

            try
            {
                assessments = await _api.GetAsync<List<AssessmentViewModel>>($"v1/RiskBenefitAssessments/all/{languageCode}");
            }
            catch (Exception ex)
            { 
            
            }
            

            Ongoing_Assessments = assessments.Where(a => a.Status != AssessmentStatus.Approved && a.Status != AssessmentStatus.Rejected)
                .ToList();

            Decided_Assessments = assessments.Where(a => a.Status == AssessmentStatus.Approved || a.Status == AssessmentStatus.Rejected)
               .ToList();


            return Page();


        }

    }
}
