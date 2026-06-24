using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BRaVe_Portal.Pages.Flagged
{
    public class IndexModel : PageModel
    {

        private readonly ILogger<IndexModel> _logger;
        private readonly IRestApiService _api;

        public IndexModel(
            ILogger<IndexModel> logger,
            IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        [BindProperty]
        public int NumberOfDistinctGroups { get; set; } = 2;

        [BindProperty]
        public string SelectedPartitionCriteria { get; set; } = string.Empty;

        public List<FlaggedBeneficiary> FlaggedBeneficiaries { get; private set; } = new();

        [BindProperty]
        public List<string> SelectedBeneficiaryIds { get; set; } = new();

        public List<SelectListItem> PartitionCriteriaOptions { get; private set; } = new();

        private bool RequestContainsPathProbeInQuery()
        {
            return Request.Query.Count > 0 &&
                   Request.Query.Any(q => StringHelper.IsPotentialPathProbe(q.Value.ToString()));
        }

        private async Task<bool> RequestContainsPathProbeInFormAsync()
        {
            if (!Request.HasFormContentType)
            {
                return false;
            }

            var form = await Request.ReadFormAsync();
            return form
                .SelectMany(entry => entry.Value)
                .Any(StringHelper.IsPotentialPathProbe);
        }

        public async Task<IActionResult> OnGet()
        {
            if (RequestContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query payload on FlaggedBeneficiaries GET.");
                return BadRequest("Invalid request.");
            }

            try
            {
                FlaggedBeneficiaries = await  LoadFlaggedBeneficiaries();
                LoadPartitionCriteriaOptions();
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occured while fetching flagged beneficiary list.");
                TempData["ErrorMessage"] = "Error occured while fetching flagged beneficiary list.";
            }


            return Page();
        }

        private void LoadPartitionCriteriaOptions()
        {
            PartitionCriteriaOptions = GetPartitionCriteria();

            if (string.IsNullOrWhiteSpace(SelectedPartitionCriteria) && PartitionCriteriaOptions.Count > 0)
            {
                SelectedPartitionCriteria = PartitionCriteriaOptions[0].Value ?? string.Empty;
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostGroupSelected()
        {
            if (RequestContainsPathProbeInQuery() || await RequestContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on GroupSelected POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                FlaggedBeneficiaries = await LoadFlaggedBeneficiaries();

                LoadPartitionCriteriaOptions();

                var eligibleBeneficiaryIds = FlaggedBeneficiaries
                    .Where(b => b.DownloadedOn is null)
                    .Select(b => b.BeneficiaryId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var beneficiaryIdsToGroup = SelectedBeneficiaryIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Where(id => eligibleBeneficiaryIds.Contains(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (beneficiaryIdsToGroup.Count == 0)
                {
                    //ModelState.AddModelError(string.Empty, "Please select at least one beneficiary before grouping.");
                    TempData["ErrorMessage"] = "Please select at least one beneficiary before grouping.";
                    return Page();
                }

                ApiResponseDto<string> result =
                 await _api.PostJsonAsync<List<string>, ApiResponseDto<string>>(
                "v1/FlaggedBeneficiaries/GroupSelected",
                beneficiaryIdsToGroup!);


                TempData["SuccessMessage"] = $"Selected beneficiaries were successfully grouped under code {result.Data}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failure occurred while grouping selected beneficiaries.");
                TempData["ErrorMessage"] = "Failure occurred while grouping selected beneficiaries.";
            }

            return RedirectToPage();

        }

        private async Task<List<FlaggedBeneficiary>> LoadFlaggedBeneficiaries()
        {
            return  await _api.GetAsync<List<FlaggedBeneficiary>>(
                    $"v1/FlaggedBeneficiaries")
                    ?? new List<FlaggedBeneficiary>();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAutoPartition()
        {
            if (RequestContainsPathProbeInQuery() || await RequestContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on ValidateActivity POST.");
                return BadRequest("Invalid request.");
            }

            try 
            {
                FlaggedBeneficiaries = await LoadFlaggedBeneficiaries();
                LoadPartitionCriteriaOptions();

                var selectListItem = PartitionCriteriaOptions.Any(option => option.Value == SelectedPartitionCriteria)
                   ? PartitionCriteriaOptions.FirstOrDefault(option => option.Value == SelectedPartitionCriteria):null;//.Text
                   //: PartitionCriteriaOptions.FirstOrDefault()?.Text ?? string.Empty;

                var validNumberOfDistinctGroups = NumberOfDistinctGroups < 2 ? 2 : NumberOfDistinctGroups;

                
                 await _api.PostJsonAsync<AutoPartitionParams, object>(
                "v1/FlaggedBeneficiaries/AutoPartition",
                new AutoPartitionParams { 
                    Groups = validNumberOfDistinctGroups,
                    Criterion = selectListItem.Value
                }!);

                // TODO: call real backend endpoint with validNumberOfDistinctGroups and partitionCriteria.
                //string code = GenerateMockGroupCode();

                TempData["SuccessMessage"] = $"Data was successfully partitioned into {NumberOfDistinctGroups} groups using '{selectListItem.Text}' as the criterion";

                
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Failed to partition the data into {NumberOfDistinctGroups} groups using selected criterion");
                //"Data partitioning failed while applying the XXX criterion");
                TempData["ErrorMessage"] = $"Data partitioning failed while applying criterion";
            }

            return RedirectToPage();

        }

        private static List<SelectListItem> GetPartitionCriteria()
        {
            // Mock backend response; replace with a real API/service call later.
            return new List<SelectListItem>
            {
                new() { Value = "alphabetical_fullname", Text = "Alphabetical by fullname" },
                new() { Value = "household_id", Text = "By householdId" },
                new() { Value = "location", Text = "By location" },
                new() { Value = "random", Text = "Random" }
            };
        }

    }
}
