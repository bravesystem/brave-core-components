using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Enumerators
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IRestApiService _api;

        public IndexModel(ILogger<IndexModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public List<EnumeratorIdGeneratorTracking> EnumeratorIdGeneratorTrackings { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public List<Enumerator> Enumerators { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public List<Enumerator> EnumeratorCodes { get; set; } = new();


        [BindProperty(SupportsGet = true)]
        public EnumeratorDto AddEnumerator { get; set; } = new();

        [BindProperty]
        public EnumeratorDto Enumerator { get; set; } = new();


        [BindProperty]
        public Enumerator UpdatedEnumerator { get; set; } = new();


        public async Task<IActionResult> OnGetAsync()
        {
            if (Request.ContainsUnexpectedQueryParameters())
            {
                _logger.LogError("Blocked suspicious query pattern on DuplicateRulesets page.");
                return BadRequest("Invalid request.");
            }

            try
            {
                Enumerators = await _api.GetAsync<List<Enumerator>>("v1/Enumerators")
                    ?? new List<Enumerator>();

                //await _cache.SetAsync(StaticKeyNames.ALL_ENUMERATORS, Enumerators);
                EnumeratorIdGeneratorTrackings = await _api.GetAsync<List<EnumeratorIdGeneratorTracking>>("v1/Enumerators/batches")
                    ?? new List<EnumeratorIdGeneratorTracking>();

                EnumeratorCodes = await _api.GetAsync<List<Enumerator>>("v1/Enumerators/EnumeratorCodes")
                    ?? new List<Enumerator>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while loading enumerator management page.");
                TempData["ErrorMessage"] = "Error occured while loading enumerator management page.";

                Enumerators = new List<Enumerator>();

                EnumeratorIdGeneratorTrackings = new List<EnumeratorIdGeneratorTracking>();

                EnumeratorCodes = new List<Enumerator>();

            }


            return Page();

        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddEnumeratorAsync()
        {
            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on AddEnumerator POST.");
                return BadRequest("Invalid request.");
            }


            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not save enumerator. Please try again.");
                //Enumerators = await _api.GetAsync<List<Enumerator>>("v1/enumerators") ?? new();
                //return Page();
                return RedirectToPage();
            }

            try
            {
                await _api.PostJsonAsync<EnumeratorDto, object>("v1/enumerators", Enumerator);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while adding enumerator.");
                TempData["ErrorMessage"] = "Error occured while adding enumerator. Please try again.";
            }

            
            return RedirectToPage();

        }



        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditEnumeratorAsync()
        {
            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on EditEnumerator POST.");
                return BadRequest("Invalid request.");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not update Enumerator. Please try again.");

                return RedirectToPage();
            }

            try
            {
                

                EnumeratorDto enumeratorDto = new EnumeratorDto()
                {
                    //EnumeratorId = UpdatedEnumerator.EnumeratorId,
                    FullName = UpdatedEnumerator.FullName,
                    IsActive = UpdatedEnumerator.IsActive,
                    IsSupervisor = UpdatedEnumerator.IsSupervisor,
                    IsPinUpdated = UpdatedEnumerator.IsPinUpdated,
                    EnumeratorPin = UpdatedEnumerator.EnumeratorPin,
                    EnumeratorCode = UpdatedEnumerator.EnumeratorCode,
                    EnumeratorType = UpdatedEnumerator.EnumeratorType,
                    Note = UpdatedEnumerator.Note
                };
                
                await _api.PutJsonAsync<EnumeratorDto, object>($"v1/enumerators/{UpdatedEnumerator.EnumeratorId}", enumeratorDto);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while updating enumerator.");
                TempData["ErrorMessage"] = "Error occured while updating enumerator. Please try again.";
            }

            return RedirectToPage();

        }

        //generating Code
        [BindProperty]
        public EnumeratorIdGeneratorTrackingDto EnumeratorIdGeneratorTracking { get; set; } = new();


        [BindProperty]
        public EnumeratorIdGeneratorTracking UpdatedEnumeratorIdGeneratorTracking { get; set; } = new();



        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddEnumeratorIdGeneratorTrackingAsync()
        {

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on AddEnumeratorIdGeneratorTrackingAsync POST.");
                return BadRequest("Invalid request.");
            }


            if (!ModelState.IsValid || EnumeratorIdGeneratorTracking!=null && !EnumeratorIdGeneratorTracking.IsValid())
            {
                //ModelState.AddModelError(string.Empty, "Could not save Enumerator Code Generator Tracking. Please try again.");

                ModelState.AddModelError(string.Empty,
                    "Could not save Enumerator Code Generator Tracking. Please ensure the following and try again: ");

                ModelState.AddModelError(string.Empty,
                    "Prefix code cannot be empty (maximum length: 5 characters);");

                ModelState.AddModelError(string.Empty,
                    "Suffix length must be greater than 0;");

                ModelState.AddModelError(string.Empty,
                    "Total code must be greater than 0.");

                //EnumeratorIdGeneratorTrackings = await _api.GetAsync<List<EnumeratorIdGeneratorTracking>>("v1/Enumerators/generate") ?? new();
                return await OnGetAsync();
            }

            try
            {
                await _api.PostJsonAsync<EnumeratorIdGeneratorTrackingDto, object>("v1/Enumerators/generate", EnumeratorIdGeneratorTracking);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occured while generating new enumerator codes.");
                TempData["ErrorMessage"] = "Error occured while generating new enumerator codes. Please try again.";
            }

            
            return RedirectToPage();

        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostResetPasswordAsync()
        {

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on ResetPasswordAsync POST.");
                return BadRequest("Invalid request.");
            }

            try
            {   
                await _api.PostJsonAsync<object>($"v1/enumerators/resetpin/{UpdatedEnumerator.EnumeratorId}");

                TempData["Success"] = "Enumerator PIN reset successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset Enumerator PIN.");
                TempData["Error"] = "Failed to reset Enumerator PIN. Please try again.";
            }

            return RedirectToPage();
        }

    }
}
