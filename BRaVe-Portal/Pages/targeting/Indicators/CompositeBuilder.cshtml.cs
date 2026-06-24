using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Services;
using BRaVe_Portal.Services.Expressions;
//using DocumentFormat.OpenXml.Office2013.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NCalc;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Pages.Targeting.Indicators
{
    public class CompositeBuilderModel : PageModel
    {
        private readonly IRestApiService _api;
        private readonly ILogger<CompositeBuilderModel> _logger;
        private readonly IAppCache _cache;
        private readonly ILookupService _lookupService;

        public CompositeBuilderModel(ILogger<CompositeBuilderModel> logger, IAppCache cache, IRestApiService api, ILookupService lookupService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

        // ================= ROUTE =================

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public bool IsEdit => Id > 0;


        // ================= MODEL =================

        [BindProperty]
        public IndicatorDto Indicator { get; set; } = new();

        public List<SelectListItem> IndicatorOptions { get; set; } = new();

        public List<Survey> Surveys { get; set; } = new();

        public List<ProgramDetails> Programs { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedProgramId { get; set; }


        // ================= EXPRESSION =================

        [BindProperty]
        public string? Expression { get; set; }

        [BindProperty]
        public bool IsValidated { get; set; }

        // ================= GET =================

        public async Task OnGetAsync()
        {
            await LoadSurveyAsync();
            await LoadAsync();

            if (!IsEdit)
                await LoadPrograms();

            if (!IsEdit && Indicator.IndicatorId == 0)
            {
                Indicator = new IndicatorDto
                {
                    IndicatorType = IndicatorType.Custom,
                    IsActive = true
                };
            }

            if (IsEdit)
                Expression = Indicator.Expression;
        }
        public async Task LoadPrograms()
        {
            // Load Programs


            Programs = await _api.GetAsync<List<ProgramDetails>>("v1/Programs") ?? new List<ProgramDetails>();
            await _cache.SetAsync($"{StaticKeyNames.ALL_PROGRAMS_IN_MISSION}{User.Tenant()}", Programs);
            _logger.LogInformation("Programs loaded from API and cached. Count: {Count}", Programs.Count);


            ViewData["Programs"] = Programs;
        }

        // ================= VALIDATE =================
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostValidateAsync()
        {

            await LoadAllAsync();

            IsValidated = false;

            //  Reserved prefix validation
            if (Indicator.IndicatorId == 0 && StartsWithKnownPrefix(Indicator.Code))
            {
                ModelState.AddModelError(
                    nameof(Expression),
                    "Invalid code: the prefix you used is reserved (calc_*, survey_*, or distr_*) and cannot be used at the beginning of a code."
                );

                TempData["ValidationMessage"] = "Validation failed.";
                TempData["ValidationStatus"] = "error";

                return Page();
            }

            // Expression required
            if (string.IsNullOrWhiteSpace(Expression))
            {
                ModelState.AddModelError(nameof(Expression), "Expression is required.");

                TempData["ValidationMessage"] = "Validation failed.";
                TempData["ValidationStatus"] = "error";

                return Page();
            }

            try
            {
                bool isValid = await ValidateExpression(Expression, Indicator.DataType);

                if (!isValid)
                {
                    ModelState.AddModelError(
                        nameof(Expression),
                        "Expression result does not match selected Data Type."
                    );

                    TempData["ValidationMessage"] = "Expression result does not match selected Data Type.";
                    TempData["ValidationStatus"] = "error";

                    return Page();
                }

                // SUCCESS
                IsValidated = true;

                TempData["ValidationMessage"] = "Expression validated successfully!";
                TempData["ValidationStatus"] = "success";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(Expression), ex.Message);

                TempData["ValidationMessage"] = ex.Message;
                TempData["ValidationStatus"] = "error";

                return Page();
            }

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostGenerateJsonAsync()
        {
            await LoadAllAsync();

            if (string.IsNullOrWhiteSpace(Expression))
            {
                ModelState.AddModelError(nameof(Expression), "Expression is required.");
                return Page();
            }

            try
            {
                string _expression = Expression;

                List<IndicatorDto> Indicators = await _api.GetAsync<List<IndicatorDto>>("v1/Indicator") ?? new List<IndicatorDto>();

                Indicator.JsonRule = ComputedExpressionParserImproved.ParseToJson(Indicators, FuncRewriter.RewriteMatches(_expression), Indicator.DataType);

                ModelState.Remove("Indicator.JsonRule");

                IsValidated = true;
            }
            catch (Exception ex)
            {
                IsValidated = false;
                ModelState.AddModelError(nameof(Expression), ex.Message);
            }

            return Page();
        }

        private async Task<bool> ValidateExpression(string expressionString, IndicatorDataType resultType)
        {

            //var normalized = NormalizeExpression(expressionString);
            var normalized = NCalcExprNormalizerSafe.NormalizeOperatorsOutsideStrings(expressionString);

            // You can also use parameters
            Expression paramExpression = new Expression(normalized);


            //paramExpression.RegisterCustomFunctions();
            paramExpression.RegisterNotFunction(); ;

            paramExpression.EvaluateFunction += (name, args) =>
            {
                if (

                name.Equals("fuzzy", StringComparison.OrdinalIgnoreCase)

                || name.Equals("like", StringComparison.OrdinalIgnoreCase)

                || name.Equals("contains", StringComparison.OrdinalIgnoreCase)

                || name.Equals("startswith", StringComparison.OrdinalIgnoreCase)

                || name.Equals("endswith", StringComparison.OrdinalIgnoreCase)

                )
                {
                    if (args.Parameters.Length < 2)
                        throw new ArgumentException("FUNC() requires 2 arguments: FUNC(value1, value2) where FUNC: fuzzy,like, contains, startswith, endswith");

                    var a = args.Parameters[0].Evaluate()?.ToString() ?? "";
                    var b = args.Parameters[1].Evaluate()?.ToString() ?? "";

                    args.Result = true;//FuzzyMatch(a, b, 0.7);
                }

            };


            List<string> indicators = getAllIndicators(normalized);

            foreach (string i in indicators)
            {
                IndicatorDataType typeId = await getIndicatorType(i);

                string param = $"{i}";

                switch (typeId)
                {

                    case IndicatorDataType.Number:
                        paramExpression.Parameters[param] = 1;
                        break;

                    case IndicatorDataType.Numeric:
                        paramExpression.Parameters[param] = 1.2;
                        break;
                    case IndicatorDataType.String:
                        paramExpression.Parameters[param] = "Test";
                        break;
                    case IndicatorDataType.Boolean:
                        paramExpression.Parameters[param] = true;
                        break;
                    case IndicatorDataType.Date:
                        paramExpression.Parameters[param] = DateTime.Now.ToShortDateString();
                        break;
                    default:
                        paramExpression.Parameters[param] = "Default";
                        break;


                }
            }

            var result = paramExpression.Evaluate();

            return evaluateBasedOnResult(result, resultType);

        }


        private bool evaluateBasedOnResult(object result, IndicatorDataType resultType)
        {
            try
            {
                switch (resultType)
                {
                    case IndicatorDataType.Number:
                        int i = (int)result;
                        return true;
                    case IndicatorDataType.Numeric:
                        double d = (double)result;
                        return true;
                    case IndicatorDataType.String:
                        string s = (string)result;
                        return true;
                    case IndicatorDataType.Boolean:
                        bool b = (bool)result;
                        return true;
                    case IndicatorDataType.Date:
                        DateTime dt = (DateTime)result;
                        return true;
                    default:
                        string ss = (string)result;
                        return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public List<string> getAllIndicators(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return new List<string>();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ordered = new List<string>();

            foreach (Match m in BracketedParam.Matches(expression))
            {
                var name = m.Groups[1].Value;
                if (name.Length == 0) continue;

                if (seen.Add(name))
                    ordered.Add(name); // preserves original casing from first occurrence
            }

            return ordered;
        }


        private static readonly Regex BracketedParam =
                new Regex(@"\[([A-Za-z0-9_]+)\]", RegexOptions.Compiled);


        Regex regex = new Regex(@"^survey_\d+_q_\d+$", RegexOptions.IgnoreCase);

        private async Task<IndicatorDataType> getIndicatorType(string code)
        {
            try
            {
                if (regex.IsMatch(code))
                    return IndicatorDataType.String;

                IndicatorDto? dto = await _api.GetAsync<IndicatorDto?>($"v1/indicator/{code}");

                return dto.DataType;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Failed to get indicator type");
                throw;
            }

        }



        private readonly Regex _prefixRegex = new(
                @"^(?:calc_\d+_(?:hh|ind)_|survey_\d+_q_\d+_|distr_\d+_)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled
            );


        public bool StartsWithKnownPrefix(string? input)
               => input is not null && _prefixRegex.IsMatch(input);




        // ================= SAVE =================

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadAllAsync();

            if (!ModelState.IsValid)
                return Page();


            if (Indicator.IndicatorId==0&&StartsWithKnownPrefix(Indicator.Code))
            {
                ModelState.AddModelError(nameof(Expression), "Invalid code: the prefix you used is reserved (calc_*, survey_*, or distr_*) and cannot be used at the beginning of a code.");
                return Page();
            }


            if (string.IsNullOrWhiteSpace(Expression))
            {
                ModelState.AddModelError(nameof(Expression), "Expression is required.");
                return Page();
            }

            try
            {
     

                //string _expression = Expression;

                Indicator.Expression = Expression;

                List<IndicatorDto> Indicators = await _api.GetAsync<List<IndicatorDto>>("v1/Indicator") ?? new List<IndicatorDto>();

                Indicator.JsonRule = ComputedExpressionParserImproved.ParseToJson(Indicators, FuncRewriter.RewriteMatches(Expression), Indicator.DataType);

                Indicator.IndicatorType = IndicatorType.Custom;

                IndicatorDto? saved;


                if (IsEdit)
                {
                    // UPDATE
                    saved = await _api.PutJsonAsync<IndicatorDto, IndicatorDto>(
                        $"v1/indicator/{Id}",
                        Indicator);
                }
                else
                {
                    // CREATE
                    Indicator.ProgramId = SelectedProgramId;
                    saved = await _api.PostJsonAsync<IndicatorDto, IndicatorDto>(
                        "v1/indicator/composite",
                        Indicator);
                }


                return RedirectToPage(
                    "/Targeting/Indicators/Indicators",
                    new { id = saved?.IndicatorId ?? Indicator.IndicatorId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(Expression), ex.Message);
                return Page();
            }
        }


        // ================= LOAD =================

        private async Task LoadAsync()
        {
            var indicators =
                await _api.GetAsync<List<IndicatorDto>>("v1/indicator") ?? new();

            // Only populate Indicator for GET requests
            if (!Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                if (IsEdit)
                {
                    Indicator = indicators.FirstOrDefault(i => i.IndicatorId == Id)
                                ?? new IndicatorDto();
                }
                else
                {
                    Indicator = new IndicatorDto
                    {
                        IndicatorType = IndicatorType.Custom,
                        IsActive = true
                    };
                }
            }

            IndicatorOptions = indicators
                .Where(i=> i.IndicatorType==IndicatorType.BuiltIn)
                .Select(i => new SelectListItem
                {
                    Value = i.Code,
                    Text = $"{i.Code} - {i.Name}"
                })
                .ToList();
        }
        private async Task LoadSurveyAsync()
        {
            try
            {
                Surveys = await _api.GetAsync<List<Survey>>($"v1/Surveys/{SelectedProgramId}") ?? new List<Survey>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load surveys for Program {SelectedProgramId}");
                throw;
            }

        }
        // ================= SURVEY QUESTIONS =================

        public async Task<IActionResult> OnGetSurveyQuestionsAsync(int surveyId)
        {
            try
            {
              
                if (surveyId == 0)
                {
                    return new JsonResult(new List<object>());
                }

                var questions = await _api.GetAsync<List<SurveyQuestionDto>>(
                    $"v1/SurveyQuestions/byId/{surveyId}"
                ) ?? new List<SurveyQuestionDto>();

                return new JsonResult(questions);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load survey questions");
                return new JsonResult(new List<object>());
            }
        }


        // ================= SURVEY ANSWERS =================

        public async Task<IActionResult> OnGetSurveyLookupValuesAsync(int lookupId)
        {
            try
            {
              
                var tenantId = User.Tenant();

                if (lookupId == null || lookupId <= 0)
                    return new JsonResult(new List<object>());

                var values = await _api.GetAsync<List<TargetingFieldValueDto>>(
                    $"v1/TargetingCriterias/lookup/{lookupId}/tenant/{tenantId}");

                return new JsonResult(values ?? new List<TargetingFieldValueDto>());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load survey lookup values");
                return new JsonResult(new List<object>());
            }
        }

        public async Task<IActionResult> OnGetLookupValuesAsync(string indicatorCode)
        {
            var tenantId = User.Tenant();
            if (string.IsNullOrWhiteSpace(indicatorCode))
                return new JsonResult(new List<object>());

            // get indicator first
            var indicator =
                await _api.GetAsync<IndicatorDto>($"v1/indicator/{indicatorCode}");

            if (indicator == null || indicator.LookUpId == null)
                return new JsonResult(new List<object>());

            // call lookup api
            var values = await _api.GetAsync<List<TargetingFieldValueDto>>(
                 $"v1/TargetingCriterias/lookup/{indicator.LookUpId}/tenant/{tenantId}");


            return new JsonResult(values ?? new());
        }


        private async Task LoadAllAsync(bool includePrograms = true)
        {
            await LoadAsync();
            await LoadSurveyAsync();

            if (includePrograms)
                await LoadPrograms();
        }


    }
}
