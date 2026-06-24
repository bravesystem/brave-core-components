using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Monitoring
{
    public class MatchingDetailsModel : PageModel
    {
        private readonly IRestApiService _api;
        private readonly ILogger<MatchingDetailsModel> _logger;

        public MatchingDetailsModel(
            IRestApiService api,
            ILogger<MatchingDetailsModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SourceUuid { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid MatchedUuid { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? JobId { get; set; }

        public BiometricMatchWithIndicatorsDto? ViewModel { get; set; }

        public List<AdjudicationDecisionDto> AdjudicationDecisions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Mode { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                string endpoint =
                Mode?.ToLower() == "manual"
                    ? $"v1/biometricMatches/manual-details?sourceUuid={SourceUuid}&matchedUuid={MatchedUuid}"
                    : $"v1/biometricMatches/details?sourceUuid={SourceUuid}&matchedUuid={MatchedUuid}";

                var match =
                    await _api.GetAsync<BiometricMatchResultDto>(endpoint);

                if (match == null)
                    return;

                var indicators =
                    await _api.GetAsync<List<DuplicateIndicatorChecklistDto>>(
                        "v1/biometricMatches/duplicate-indicators?languageCode=en")
                    ?? new();

                var decisions =
                    await _api.GetAsync<ApiResponseDto<List<AdjudicationDecisionDto>>>(
                        "v1/adjudication/decisions");

                AdjudicationDecisions = decisions?.Data ?? new();

                /* ================= PROGRAMMATIC DATA ================= */

                var sourceProgram =
                    await _api.GetAsync<List<ProgrammaticDataDto>>(
                        $"v1/biometricMatches/programmatic-data?householdId={match.SourceHouseholdId}")
                    ?? new();

                var matchedProgram =
                    await _api.GetAsync<List<ProgrammaticDataDto>>(
                        $"v1/biometricMatches/programmatic-data?householdId={match.MatchedHouseholdId}")
                    ?? new();


                /* ================= APPLY INDICATOR LOGIC ================= */

                var computedIndicators =
                    SetIndicators(match, indicators);

                ViewModel = new BiometricMatchWithIndicatorsDto
                {
                    Match = match,
                    DuplicateIndicators = computedIndicators,
                    SourceProgrammaticData = sourceProgram,
                    MatchedProgrammaticData = matchedProgram
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading match details");
            }
        }

        private List<DuplicateIndicatorChecklistDto> SetIndicators(BiometricMatchResultDto match,List<DuplicateIndicatorChecklistDto> duplicateIndicators)
        {
            List<DuplicateIndicatorChecklistDto> result = new();

            foreach (var i in duplicateIndicators)
            {
                var indicator = new DuplicateIndicatorChecklistDto
                {
                    Id = i.Id,
                    IndicatorId = i.IndicatorId,
                    TenantId = i.TenantId,
                    ExclusiveGroupId = i.ExclusiveGroupId,
                    Score = i.Score,
                    Name = i.Name
                };

                if (new int[]
                {
            DefaultIndicators.MatchingSomeMembers,
            DefaultIndicators.MathingAllMembers,
            DefaultIndicators.MembersNotMatching
                }.Contains(indicator.IndicatorId))
                {
                    indicator.Disabled = false;
                }

                result.Add(indicator);
            }

            if (match.HighScore)
                result.First(x => x.IndicatorId == DefaultIndicators.HighScore).Selected = true;
            else if (match.LowScore)
                result.First(x => x.IndicatorId == DefaultIndicators.NotHighScore).Selected = true;
            else
                result.First(x => x.IndicatorId == DefaultIndicators.ManualDeduplication).Selected = true;

            if (match.SameGender)
                result.First(x => x.IndicatorId == DefaultIndicators.MatchingGender).Selected = true;
            else
                result.First(x => x.IndicatorId == DefaultIndicators.GenderNotMatching).Selected = true;

            if (match.SameAge)
                result.First(x => x.IndicatorId == DefaultIndicators.MatchingAge).Selected = true;
            else if (match.AgeGapBelow5)
                result.First(x => x.IndicatorId == DefaultIndicators.AgeGapLt5).Selected = true;
            else if (match.AgeGapBetween5and10)
                result.First(x => x.IndicatorId == DefaultIndicators.AgeGapElse).Selected = true;
            else if (match.AgeGapOver10)
                result.First(x => x.IndicatorId == DefaultIndicators.AgeGapGt10).Selected = true;

            if (AlgoUtils.SimilarNames(match.SourceFullName, match.MatchedFullName))
                result.First(x => x.IndicatorId == DefaultIndicators.MatchingNames).Selected = true;
            else
                result.First(x => x.IndicatorId == DefaultIndicators.NamesNotMatching).Selected = true;

            if (!match.MatchingFamSize)
                result.First(x => x.IndicatorId == DefaultIndicators.MembersNotMatching).Selected = true;

            return result;
        }

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> OnPostAdjudicateAsync([FromBody] AdjudicationRequestDto request)
        {
            try
            {
                await _api.PostJsonAsync<object, ApiResponseDto<object>>(
                    "v1/adjudication/adjudicate",
                    request);

                TempData["SuccessMessage"] = "Adjudication completed successfully.";

                var redirectUrl = request.JobId.HasValue
                    ? Url.Page("/DuplicateRulesets/DeduplicationResults",
                        new { jobId = request.JobId })
                    : Url.Page("/Monitoring/BiometricMatchComparison");

                return new JsonResult(new { redirect = redirectUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Adjudication failed");

                var redirectUrl = request.JobId.HasValue
                    ? Url.Page("/DuplicateRulesets/DeduplicationResults",
                        new { jobId = request.JobId })
                    : Url.Page("/Monitoring/BiometricMatchComparison");

                return new JsonResult(new { redirect = redirectUrl });
            }
        }




        public async Task<IActionResult> OnGetHouseholdMembersAsync(string householdId)
        {
            try
            {
                var members =
                    await _api.GetAsync<List<HouseholdMemberDto>>(
                        $"v1/dashboard/households/{householdId}/members")
                    ?? new List<HouseholdMemberDto>();

                return new JsonResult(members);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed loading household members for {HouseholdId}", householdId);
                return new JsonResult(new List<HouseholdMemberDto>());
            }
        }


        public async Task<IActionResult> OnGetMatchHistoryAsync(Guid memberUuid)
        {
            try
            {
                var response =
                    await _api.GetAsync<ApiResponseDto<List<MemberMatchHistoryDto>>>(
                        $"v1/adjudication/member-match-history/{memberUuid}");

                return new JsonResult(response?.Data ?? new List<MemberMatchHistoryDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed loading match history");
                return new JsonResult(new List<MemberMatchHistoryDto>());
            }
        }
    }
}