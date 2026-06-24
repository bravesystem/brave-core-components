using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Services.Expressions;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NCalc;
using System.Text.Json;
using System.Text.RegularExpressions;
using BRaVe_Portal.Models;

namespace BRaVe_Portal.Pages.Targeting.Criterias
{
    public class CriteriaBuilderModel : PageModel
    {
        private readonly TargetingValidationService _validationService;
        private readonly IRestApiService _api;

        public CriteriaBuilderModel(IRestApiService api, TargetingValidationService validationService)
        {
            _api = api;
            _validationService = validationService;
        }

        // ================= ROUTE =================

        [BindProperty(SupportsGet = true)]
        public int TargetingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CriteriaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal TotalWeight { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool WeightBound { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal TotalCriteriaWeight { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RuleName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ProgramId { get; set; }
        // ================= MODEL =================

        [BindProperty]
        public IndicatorDto Indicator { get; set; } = new();

        [BindProperty]
        public TargetingCriteriaDto Criteria { get; set; } = new();

        [BindProperty]
        public List<TargetingCriteriaDto> TargetingCriteria { get; set; } = new();


        [BindProperty]
        public List<TargetingFieldsDto> TargetingFields { get; private set; } = new();
        public List<SelectListItem> IndicatorOptions { get; set; } = new();


        // ================= EXPRESSION =================

        [BindProperty]
        public string? Expression { get; set; }

        [BindProperty]
        public bool IsValidated { get; set; }

        // ================= SURVEYS =================

        public List<Survey> Surveys { get; set; } = new();

        // ================= GET =================

        public async Task OnGetAsync()
        {
            await LoadAsync();
            await LoadSurveyAsync();

            // Always set TargetingId
            Criteria.TargetingId = TargetingId;

            // CREATE MODE
            if (CriteriaId == 0)
            {
                Criteria.Score = 1;
                Criteria.Weight = 1;
                return;
            }

            // EDIT MODE
            var existing =
                await _api.GetAsync<TargetingCriteriaDto>(
                    $"v1/TargetingCriterias/{CriteriaId}");

            if (existing != null)
            {
                Criteria = existing;
                Expression = existing.Expression;
            }

            TargetingFields =
                await _api.GetAsync<List<TargetingFieldsDto>>(
                    "v1/TargetingRules/fields") ?? new();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostValidateAsync()
        {
            await LoadAsync();
            await LoadSurveyAsync();

            Criteria.JsonRule = null;
            IsValidated = false;

            if (string.IsNullOrWhiteSpace(Expression))
            {
                ModelState.AddModelError(nameof(Expression), "Expression is required.");
                return Page();
            }

            var dataType = IndicatorDataType.Boolean;

            try
            {
                bool isValid = await ValidateExpression(Expression, dataType);

                if (!isValid)
                {
                    ModelState.AddModelError(
                        nameof(Expression),
                        "Expression result does not match selected Data Type."
                    );

                    TempData["ValidationMessage"] = "Validation failed.";
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
            await LoadAsync();
            await LoadSurveyAsync();

            if (string.IsNullOrWhiteSpace(Expression))
            {
                ModelState.AddModelError(nameof(Expression), "Expression is required.");
                return Page();
            }

            try
            {
                string _expression = Expression;

                Criteria.JsonRule = ExpressionCriteriaFormatter.ToCriteriaJson(_expression, Convert.ToDouble(Criteria.Score * Criteria.Weight));
                TempData["ValidationMessage"] = "Json Generated successfully!";
                TempData["ValidationStatus"] = "success";
                ModelState.Remove("Criteria.JsonRule");
                IsValidated = true;
            }
            catch (Exception ex)
            {
                TempData["ValidationMessage"] = "Failed to create json. Check your Expression and try again.";
                TempData["ValidationStatus"] = "error";
                IsValidated = false;
                ModelState.AddModelError(nameof(Expression), ex.Message);
            }

            return Page();
        }

        private static readonly Regex SurveyQuestionTokenRegex =
            new Regex(@"\[([A-Za-z0-9]+)\s*_\s*([0-9]+)\]", RegexOptions.Compiled);

        private async Task<bool> ValidateExpression(string expressionString, IndicatorDataType resultType)
        {
            // Replace survey question tokens like [59ZG - 1] with a numeric placeholder
            // so NCalc can evaluate the structure of the expression safely.
            var safeExpressionString = SurveyQuestionTokenRegex.Replace(expressionString, "1");

            //var normalized = NormalizeExpression(expressionString);
            var normalized = NCalcExprNormalizerSafe.NormalizeOperatorsOutsideStrings(safeExpressionString);

            // You can also use parameters
            Expression paramExpression = new Expression(normalized);


            paramExpression.RegisterNotFunction(); ;
            //paramExpression.RegisterCustomFunctions();

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
                // Skip any tokens that look like survey-question placeholders
                // e.g. parts of [59ZG - 1]. We only want real indicator codes here.
                //if (SurveyQuestionTokenRegex.IsMatch($"[{i}]"))
                //    continue;

                var typeId = await getIndicatorType(i);
                if (typeId == null)
                    continue;

                var param = $"{i}";

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


        /*public async Task<IActionResult> OnGetSurveyQuestionsAsync(string surveyCode)
        {
            if (string.IsNullOrWhiteSpace(surveyCode))
            {
                return new JsonResult(new List<object>());
            }

            var allQuestions = await _api.GetAsync<List<SurveyQuestionDto>>(
                $"v1/SurveyQuestions/{surveyCode}"
            ) ?? new List<SurveyQuestionDto>();

            return new JsonResult(allQuestions);
        }*/

        public async Task<IActionResult> OnGetSurveyQuestionsAsync(int surveyId)
        {
            
            if (surveyId==0)
            {
                return new JsonResult(new List<object>());
            }

            var allQuestions = await _api.GetAsync<List<SurveyQuestionDto>>(
                $"v1/SurveyQuestions/byId/{surveyId}"
            ) ?? new List<SurveyQuestionDto>();

            return new JsonResult(allQuestions);
        }
        public async Task<IActionResult> OnGetSurveyQuestionsAnswearsAsync(int TenantId, int LookUpId)
        {

            if (TenantId == 0)
            {
                return new JsonResult(new List<object>());
            }

            var allQuestionsAnswears = await _api.GetAsync<List<SurveyQuestionDto>>(
                $"v1/SurveyQuestions/lookup/{LookUpId}/tenant/{TenantId}"
            ) ?? new List<SurveyQuestionDto>();

            return new JsonResult(allQuestionsAnswears);
        }

        Regex regex = new Regex(@"^survey_\d+_q_\d+$", RegexOptions.IgnoreCase);

        private async Task<IndicatorDataType?> getIndicatorType(string code)
        {
            try
            {
                if (regex.IsMatch(code))
                    return IndicatorDataType.String;


                IndicatorDto? dto = await _api.GetAsync<IndicatorDto?>($"v1/indicator/{code}");

                if (dto == null)
                    return null;

                return dto.DataType;
            }
            catch
            {
                // If the indicator is not found or the API fails, treat it as unknown
                // and let the caller decide to skip it.
                return null;
            }
        }


        // ================= SAVE =================
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadAsync();
            await LoadSurveyAsync();

            // Set CreatedBy if new
            if (Criteria.Id == 0)
                Criteria.CreatedBy = User.Identifier();

            if (!ModelState.IsValid)
                return Page();

            if (string.IsNullOrWhiteSpace(Expression))
            {
                ModelState.AddModelError(nameof(Expression), "Expression is required.");
                return Page();
            }

            try
            {
                // Parse expression and generate JSON rule
                var tree = CriteriaExpressionParser.Parse(Expression);
                Criteria.Expression = Expression;

                Criteria.JsonRule = ExpressionCriteriaFormatter.ToCriteriaJson(Expression, Convert.ToDouble(Criteria.Score * Criteria.Weight));

                Indicator.IndicatorType = IndicatorType.Custom;
                TargetingCriteriaDto? saved;

                if (Criteria.Id > 0)
                {
                    // UPDATE
                    saved = await _api.PutJsonAsync<TargetingCriteriaDto, TargetingCriteriaDto>(
                        $"v1/TargetingCriterias/{Criteria.Id}",
                        Criteria);
                }
                else
                {
                    // CREATE
                    saved = await _api.PostJsonAsync<TargetingCriteriaDto, TargetingCriteriaDto>(
                        "v1/TargetingCriterias",
                        Criteria);
                }

                return RedirectToPage(
                    "/Targeting/Criterias/Index",
                    new
                    {
                        ID = saved?.TargetingId ?? Criteria.TargetingId,
                        ProgramId,
                        Description = saved?.Description ?? Criteria.Description,
                        TotalWeight = TotalWeight,
                        WeightBound = WeightBound,
                        TotalCriteriaWeight = TotalCriteriaWeight,
                        RuleName = RuleName
                    });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(Expression), ex.Message);
                return Page();
            }
        }


        // ================= LOAD =================
        public async Task<IActionResult> OnGetLookupValuesAsync(string indicatorCode)
        {
            try
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
            catch (Exception ex)
            {
                // log the error (assuming ILogger is injected)
              

                // return safe fallback response
                return new JsonResult(new List<object>());
            }
        }
        public async Task<IActionResult> OnGetLookupValuesByLookupIdAsync(int? lookupId)
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
            catch (Exception)
            {
                return new JsonResult(new List<object>());
            }
        }

        private async Task LoadAsync()
        {
            var indicators =
                await _api.GetAsync<List<IndicatorDto>>("v1/indicator") ?? new();



            IndicatorOptions = indicators
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
                Surveys = await _api.GetAsync<List<Survey>>($"v1/Surveys/{ProgramId}") ?? new List<Survey>();
            }

            catch (Exception ex)
            {
                throw new Exception();
            }

        }

    }
}
