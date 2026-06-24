using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2013.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace BRaVe_Portal.Pages.surveys.Questionnaire
{
    public class QuestionBuilderModel : PageModel
    {
        private readonly ILogger<QuestionBuilderModel> _logger;
        private readonly IAppCache _cache;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public QuestionBuilderModel(ILogger<QuestionBuilderModel> logger, IAppCache cache, IRestApiService api, ILookupService lookupService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

        public class QuestionModel
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public int? SourceId { get; set; }   // maps to SurveyQuestionDto.Id if available
            public int? QuestionOrder { get; set; }
            public string Text { get; set; } = string.Empty;
            public string Type { get; set; } = "";      // will store AnswerType as string (Id)
            public string TypeName { get; set; } = "";  // friendly name from lookups
            public int? LookupId { get; set; }
            public int? DatasetId { get; set; }
            public short? MinSelection { get; set; }
            public short? MaxSelection { get; set; }
            public bool Required { get; set; } = false;
            public bool Active { get; set; } = true;
            public string Expression { get; set; } = string.Empty;

            // passthrough fields
            public string? Restriction { get; set; }
            public string? SkipLogic { get; set; }
            public string? ResultExpression { get; set; }
            public Dictionary<string, object>? Restrictions { get; set; }


        }

        // Public so view can read
        public List<QuestionModel> Questions { get; set; } = new List<QuestionModel>();

        [BindProperty(SupportsGet = true)]
        public int SurveyId { get; set; }

        [BindProperty] public SurveyQuestionDto ExistingQuestion { get; set; }

        public List<LookupTableNameDto> LookupNames { get; set; } = new List<LookupTableNameDto>();

        public Models.Survey? CurrentSurvey { get; set; }

        public string TargetQuestionText { get; set; } = string.Empty;


        public string[] Types { get; } = new[] { "Single-Line Input", "Multi-Line Input", "Dropdown", "Radio", "Checkbox", "Number", "Date" };

        public List<DatasetsDto> Datasets { get; set; } = new();

        public List<AdministrativeLevel> AdministrativeLevels { get; set; } = new();

        public class DefaultValueOption
        {
            public string Key { get; set; } = "";
            public string Label { get; set; } = "";
            public List<string> Types { get; set; } = new();
        }

        public List<DefaultValueOption> DefaultValueOptions { get; set; } = new()
        {
            new DefaultValueOption
            {
                Key = "$age",
                Label = "Age",
                Types = new List<string>{ "INT", "NUMERIC" }
            },
            new DefaultValueOption
            {
                Key = "$firstname",
                Label = "First Name",
                Types = new List<string>{ "TEXT" }
            },
            new DefaultValueOption
            {
                Key = "$lastname",
                Label = "Last Name",
                Types = new List<string>{ "TEXT" }
            },
            new DefaultValueOption
            {
                Key = "$phone",
                Label = "Phone Number",
                Types = new List<string>{ "TEXT" }
            }
        };

       

        // GET: load survey and existing questions
        // fixed
        public async Task<IActionResult> OnGetAsync(int surveyId, int questionId, string surveyCode)
        {
            if (Request.ContainsPathProbeInQuery() || StringHelper.IsPotentialPathProbe(surveyCode))
            {
                _logger.LogWarning("Blocked suspicious payload on QuestionBuilder GET.");
                return BadRequest("Invalid request.");
            }

            Questions.Clear();

            _logger.LogInformation("OnGetAsync called: SurveyId={SurveyId}, SurveyCode={SurveyCode}, QuestionId={QuestionId}", surveyId, surveyCode, questionId);

            SurveyId = surveyId;
            int tenantId = User.Tenant();

            try
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                CoreLookups coreLookups = null;
                try
                {
                    coreLookups = await _lookupService.GetCoreLookups(languageCode);
                    ViewData["CoreLookups"] = coreLookups;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to load core lookups (language={Language})", languageCode);
                }


                //Load lookup table
                try
                {
                    LookupNames = await _api.GetAsync<List<LookupTableNameDto>>("v1/Lookups/lookups") ?? new();
                    ViewData["LookupNames"] = LookupNames;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to load lookup names for survey {SurveyId}", SurveyId);
                }

                try
                {
                    Datasets = (await _api.GetAsync<List<DatasetsDto>>("v1/Datasets"))
                               ?.Where(d => d.IsActive)
                               .ToList()
                               ?? new();

                    ViewData["Datasets"] = Datasets;

                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to load datasets");
                }


                //Get administrative levels
                try
                {
                    AdministrativeLevels =
                        (await _api.GetAsync<List<AdministrativeLevel>>("v1/AdministrativeLevels"))
                        ?.Where(a => a.IsActive)
                        .ToList()
                        ?? new();

                    ViewData["AdministrativeLevels"] = AdministrativeLevels;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to load administrative levels");
                }

                _logger.LogInformation("Fetching survey details for SurveyId={SurveyId}", SurveyId);
                CurrentSurvey = await _api.GetAsync<Models.Survey>($"v1/Surveys/Survey/{SurveyId}");

                if (CurrentSurvey != null)
                {
                    _logger.LogInformation("Fetched survey successfully: SurveyCode={SurveyCode}", CurrentSurvey.SurveyCode);

                    var allQuestions = await _api.GetAsync<List<SurveyQuestionDto>>(
                        $"v1/SurveyQuestions/{CurrentSurvey.SurveyCode}"
                    ) ?? new List<SurveyQuestionDto>();

                    _logger.LogInformation("Fetched {Count} total questions for survey {SurveyCode}", allQuestions.Count, CurrentSurvey.SurveyCode);

                    // --------------------------------------------------------------------
                    // NEW: Set TargetQuestionText immediately for Razor header
                    if (questionId > 0)
                    {
                        var target = allQuestions.FirstOrDefault(q => q.Id == questionId);
                        if (target != null)
                        {
                            ExistingQuestion = target;
                            TargetQuestionText = target.QuestionText ?? string.Empty;
                        }
                    }
                    // --------------------------------------------------------------------

                    // build map: answerType id -> friendly text
                    var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (coreLookups?.QuestionTypes != null)
                    {
                        foreach (var lt in coreLookups.QuestionTypes)
                        {
                            try
                            {
                                var key = (lt?.Id ?? 0).ToString();
                                if (!typeMap.ContainsKey(key))
                                {
                                    typeMap[key] = lt?.Text ?? lt?.ToString() ?? key;
                                }
                            }
                            catch { }
                        }
                    }

                    // Map DTO -> client QuestionModel
                    Questions = allQuestions
                        .OrderBy(q => q.QuestionOrder ?? int.MaxValue)
                        .Select(q => {
                            var typeKey = (q.AnswerType > 0) ? q.AnswerType.ToString() : "";
                            return new QuestionModel
                            {
                                SourceId = q.Id,
                                QuestionOrder = q.QuestionOrder,
                                Text = q.QuestionText ?? string.Empty,
                                Type = typeKey,
                                TypeName = typeMap.TryGetValue(typeKey, out var tn) ? tn : (typeKey == "" ? "" : typeKey),
                                LookupId = q.LookupId,
                                DatasetId = q.DatasetId,
                                MinSelection = q.MinSelection,
                                MaxSelection = q.MaxSelection,
                                Required = q.IsRequired,
                                Active = q.IsActive,
                                Expression = q.ResultExpression ?? string.Empty,
                                Restriction = q.Restriction,
                                SkipLogic = q.SkipLogic,
                                ResultExpression = q.ResultExpression,
                                Restrictions = !string.IsNullOrWhiteSpace(q.Restriction)
                                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(q.Restriction)
                                    : null,

                            };
                        }).ToList();
                }
                else
                {
                    _logger.LogWarning("Survey with ID={SurveyId} not found", SurveyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading survey and questions for SurveyId={SurveyId}", SurveyId);
            }

            return Page();
        }


        // GET handler: returns questions for the survey as JSON
        // URL: /surveys/Questionnaire/QuestionBuilder?handler=questions&surveyId={id}
        // GET handler: returns questions for the survey as JSON
        // URL: /surveys/Questionnaire/QuestionBuilder?handler=questions&surveyId={id}
        // Added: optional questionId so the handler can identify the active question being edited
        public async Task<JsonResult> OnGetQuestionsAsync(int surveyId, int questionId = 0)
        {
            var result = new List<QuestionModel>();

            if (surveyId <= 0)
            {
                return new JsonResult(result);
            }

            if (Request.ContainsPathProbeInQuery() )
            {
                _logger.LogError("Blocked suspicious payload on GetQuestionsAsync GET.");
                return new JsonResult(result);
            }

            try
            {
                Models.Survey? survey = null;
                try
                {
                    survey = await _api.GetAsync<Models.Survey>($"v1/Surveys/Survey/{surveyId}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load survey for Questions API: SurveyId={SurveyId}", surveyId);
                }

                if (survey == null || string.IsNullOrWhiteSpace(survey.SurveyCode))
                {
                    return new JsonResult(result);
                }

                var allQuestions = await _api.GetAsync<List<SurveyQuestionDto>>(
                    $"v1/SurveyQuestions/{survey.SurveyCode}"
                ) ?? new List<SurveyQuestionDto>();

                // --------------------------------------------------------------------
                // NEW: Set TargetQuestionText so JS calls can update page header
                if (questionId > 0)
                {
                    var target = allQuestions.FirstOrDefault(q => q.Id == questionId);
                    if (target != null)
                    {
                        ExistingQuestion = target;
                        TargetQuestionText = target.QuestionText ?? string.Empty;
                    }
                }
                // --------------------------------------------------------------------

                CoreLookups? coreLookups = null;
                try
                {
                    coreLookups = await _lookupService.GetCoreLookups(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load core lookups for Questions API");
                }

                //Get lookup table values
                try
                {
                    LookupNames = await _api.GetAsync<List<BRaVe_Management_Backend.DTOs.LookupTableNameDto>>("v1/Lookups/lookups") ?? new();
                    ViewData["LookupNames"] = LookupNames;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load lookup names for Questions API");
                }

                var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (coreLookups?.QuestionTypes != null)
                {
                    foreach (var lt in coreLookups.QuestionTypes)
                    {
                        try
                        {
                            var key = (lt?.Id ?? 0).ToString();
                            if (!typeMap.ContainsKey(key))
                                typeMap[key] = lt?.Text ?? lt?.ToString() ?? key;
                        }
                        catch { }
                    }
                }

                result = allQuestions
                    .OrderBy(q => q.QuestionOrder ?? int.MaxValue)
                    .Select(q =>
                    {
                        var typeKey = (q.AnswerType > 0) ? q.AnswerType.ToString() : "";
                        return new QuestionModel
                        {
                            Id = Guid.NewGuid().ToString(),
                            SourceId = q.Id,
                            QuestionOrder = q.QuestionOrder,
                            Text = q.QuestionText ?? string.Empty,
                            Type = typeKey,
                            TypeName = typeMap.TryGetValue(typeKey, out var tn) ? tn : (typeKey == "" ? "" : typeKey),
                            LookupId = q.LookupId,
                            DatasetId = q.DatasetId,
                            MinSelection = q.MinSelection,
                            MaxSelection = q.MaxSelection,
                            Required = q.IsRequired,
                            Active = q.IsActive,
                            Expression = q.ResultExpression ?? string.Empty,
                            Restriction = q.Restriction,
                            SkipLogic = q.SkipLogic,
                            ResultExpression = q.ResultExpression,
                            Restrictions = !string.IsNullOrWhiteSpace(q.Restriction)
                                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(q.Restriction)
                                : null,

                        };
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Questions API for SurveyId={SurveyId}", surveyId);
            }

            return new JsonResult(result);
        }


        // ACTIVATE SURVEY
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostActivateSurveyAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on ActivateSurvey POST.");
                return BadRequest("Invalid request.");
            }

            _logger.LogInformation("OnPostActivateSurveyAsync called for SurveyId={SurveyId}", SurveyId);

            try
            {
                if (SurveyId == 0)
                {
                    _logger.LogWarning("Invalid SurveyId supplied for activation");
                    TempData["ErrorMessage"] = "Invalid survey ID.";
                    return RedirectToPage("/Surveys/Index");
                }

                _logger.LogInformation("Calling API to activate survey {SurveyId}", SurveyId);
                await _api.PutJsonAsync<object, object>($"v1/Surveys/activate/{SurveyId}", null, CancellationToken.None);

                _logger.LogInformation("Survey activated successfully: SurveyId={SurveyId}", SurveyId);
                TempData["SuccessMessage"] = "Survey activated successfully.";
                return RedirectToPage("/surveys/Questionnaire/QuestionBuilder", new { SurveyId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating survey {SurveyId}", SurveyId);
                TempData["ErrorMessage"] = "Failed to activate survey. Please try again.";
                return RedirectToPage("/surveys/Questionnaire/QuestionBuilder", new { SurveyId });
            }
        }

        // Save handler receives JSON array of questions posted from client.

        private static readonly object _lock = new object();
        private static bool _isRunning = false;



        public async Task<IActionResult> OnPostSave([FromBody] List<QuestionModel> questions)
        {
            if (Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync()
                //JsonPayloadHelper.ContainsPathProbe(questions)
                )
            {
                _logger.LogError("Blocked suspicious payload on Save questions POST.");
                return BadRequest("Invalid request.");
            }

            lock (_lock)
            {
                if (_isRunning)
                {
                    return Page();
                }
                _isRunning = true;
            }



            if (questions == null || !questions.Any())
            {
                TempData["ErrorMessage"] = "No questions provided.";

                lock (_lock)
                {
                    _isRunning = false;
                }

                return new JsonResult(new
                {
                    redirectUrl = Url.Page("/surveys/Questionnaire/QuestionBuilder", new { SurveyId })
                });
            }

            // ensure CurrentSurvey is populated (may be null on POST)
            if (CurrentSurvey == null && SurveyId > 0)
            {
                try
                {
                    CurrentSurvey = await _api.GetAsync<Models.Survey>($"v1/Surveys/Survey/{SurveyId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load survey details for SurveyId={SurveyId} during OnPostSave", SurveyId);

                    TempData["ErrorMessage"] = "Failed to load survey details for SurveyId={SurveyId} during OnPostSave";

                    lock (_lock)
                    {
                        _isRunning = false;
                    }

                    return Page();
                }
            }

            if (CurrentSurvey == null || string.IsNullOrWhiteSpace(CurrentSurvey.SurveyCode))
            {
                // Can't proceed safely without survey code
                TempData["ErrorMessage"] = "Unable to determine survey code for saving questions.";

                lock (_lock)
                {
                    _isRunning = false;
                }

                return new JsonResult(new
                {
                    redirectUrl = Url.Page("/surveys/Questionnaire/QuestionBuilder", new { SurveyId })
                });
            }

            var dtoList = questions.Select(q =>
            {
                int answerType = 0;
                if (!string.IsNullOrEmpty(q.Type))
                {
                    int.TryParse(q.Type, out answerType);
                }

                return new SurveyQuestionDto
                {
                    SurveyCode = CurrentSurvey.SurveyCode,
                    Id = q.SourceId,
                    QuestionOrder = q.QuestionOrder,
                    QuestionText = q.Text,
                    AnswerType = answerType,
                    LookupId = q.LookupId,
                    DatasetId = q.DatasetId,
                    MinSelection = q.MinSelection,
                    MaxSelection = q.MaxSelection,
                    IsRequired = q.Required,
                    IsActive = q.Active,
                    ResultExpression = q.Expression,
                    Restriction = q.Restriction,
                    SkipLogic = q.SkipLogic
                };
            }).ToList();

            int saved = 0;
            var errors = new List<string>();
            var tasks = new List<Task>();

            bool errorHappened = false;
            string errorMessage = string.Empty;

            foreach (var dto in dtoList)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (dto.Id.HasValue && dto.Id.Value > 0)
                        {
                            await _api.PutJsonAsync<SurveyQuestionDto, object>(
                                $"v1/SurveyQuestions/update/{dto.SurveyCode}/{dto.Id}",
                                dto);
                        }
                        else
                        {
                            await _api.PostJsonAsync<SurveyQuestionDto, object>(
                                "v1/SurveyQuestions/create",
                                dto);
                        }

                        Interlocked.Increment(ref saved);
                    }
                    catch (Exception ex)
                    {

                        _logger.LogError(ex,
                            "Failed to persist question (SurveyCode={SurveyCode}, Id={Id})",
                            dto.SurveyCode, dto.Id);

                        if (!errorHappened)
                        {
                            errorHappened = true;

                            errorMessage = $"Failed to persist question (SurveyCode={dto.SurveyCode}, Id={dto.Id})";
                        }



                    }


                }));

                if (errorHappened)
                {
                    TempData["ErrorMessage"] = errorMessage;


                    lock (_lock)
                    {
                        _isRunning = false;
                    }

                    return Page();

                }


            }

            await Task.WhenAll(tasks);

            if (errors.Any())
            {
                TempData["ErrorMessage"] = $"Saved {saved} / {dtoList.Count} questions. Failed: {string.Join(", ", errors)}";
            }
            else
            {
                TempData["SuccessMessage"] = $"{saved} questions saved successfully.";
            }

            lock (_lock)
            {
                _isRunning = false;
            }

            return new JsonResult(new
            {
                success = true,
                redirectUrl = Url.Page("/surveys/Questionnaire/QuestionBuilder", new { SurveyId })
            });
        }

        // Delete question
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteQuestionAsync(int QuestionId, string surveyCode, int surveyId)
        {

            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync() ||
                StringHelper.IsPotentialPathProbe(surveyCode))
            {
                _logger.LogError("Blocked suspicious surveyCode pattern on delete question. SurveyId={SurveyId}", surveyId);
                return BadRequest("Invalid request.");
            }

            _logger.LogInformation("OnPostDeleteQuestionAsync called: QuestionId={QuestionId}, SurveyCode={SurveyCode}, SurveyId={SurveyId}", QuestionId, surveyCode, surveyId);

            try
            {
                await _api.DeleteAsync($"v1/SurveyQuestions/delete/{surveyCode}/{QuestionId}");

                TempData["SuccessMessage"] = "Survey question deleted successfully.";
                return RedirectToPage("/surveys/Questionnaire/QuestionBuilder", new { SurveyId = surveyId });
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning(ex, "Soft delete triggered for QuestionId={QuestionId}", QuestionId);

                TempData["ErrorMessage"] = ex.Message;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question QuestionId={QuestionId}", QuestionId);

                TempData["ErrorMessage"] = "Failed to delete the question. Please try again.";
            }

            return RedirectToPage("/surveys/Questionnaire/QuestionBuilder", new { SurveyId = surveyId });

        }


        // POST handler: saves the skiplogic JSON for a single question (AJAX)
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostSaveSkipLogicAsync(int questionId, int surveyId, [FromBody] object skipLogic)
        {
            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync() 
                //ContainsPathProbeInJsonPayload(skipLogic)
            )
            {
                _logger.LogError("Blocked suspicious payload on SaveSkipLogic POST.");
                Response.StatusCode = 400;
                return new JsonResult(new { success = false, message = "Invalid request." });
            }

            if (questionId <= 0)
            {
                return new JsonResult(new { success = false, message = "Invalid question id." });
            }

            try
            {
                // Ensure CurrentSurvey is loaded
                if (CurrentSurvey == null && surveyId > 0)
                {
                    try
                    {
                        CurrentSurvey = await _api.GetAsync<Models.Survey>($"v1/Surveys/Survey/{surveyId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load survey for SaveSkipLogic: SurveyId={SurveyId}", surveyId);
                    }
                }

                if (CurrentSurvey == null || string.IsNullOrWhiteSpace(CurrentSurvey.SurveyCode))
                {
                    return new JsonResult(new { success = false, message = "Survey not found." });
                }

                // Load existing questions for the survey and find target
                var allQuestions = await _api.GetAsync<List<SurveyQuestionDto>>($"v1/SurveyQuestions/{CurrentSurvey.SurveyCode}") ?? new List<SurveyQuestionDto>();
                var target = allQuestions.FirstOrDefault(q => q.Id == questionId);

                if (target == null)
                {
                    return new JsonResult(new { success = false, message = "Question not found." });
                }

                // Convert incoming object back to canonical JSON string for storage
                string? serialized = null;

                // Detect empty root group
                if (skipLogic is System.Text.Json.JsonElement je &&
                    je.ValueKind == System.Text.Json.JsonValueKind.Object &&
                    je.TryGetProperty("expressions", out var expressions) &&
                    expressions.ValueKind == System.Text.Json.JsonValueKind.Array &&
                    expressions.GetArrayLength() == 0)
                {
                    // Empty builder , store NULL
                    serialized = null;
                }
                else
                {
                    serialized = System.Text.Json.JsonSerializer.Serialize(skipLogic);
                }

                target.SkipLogic = serialized;

                // Persist update via API update endpoint (existing pattern)
                await _api.PutJsonAsync<SurveyQuestionDto, object>($"v1/SurveyQuestions/update/{CurrentSurvey.SurveyCode}/{target.Id}", target);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save skiplogic for QuestionId={QuestionId}, SurveyId={SurveyId}", questionId, surveyId);
                return new JsonResult(new { success = false, message = "Failed to save skip logic." });
            }
        }

        // POST handler: saves the restrictions JSON for a single question (AJAX)
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostSaveRestrictionsAsync(int questionId, int surveyId, [FromBody] object restrictions)
        {
            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync() 
                //ContainsPathProbeInJsonPayload(restrictions)
                    )
            {
                _logger.LogError("Blocked suspicious payload on SaveRestrictions POST.");
                Response.StatusCode = 400;
                return new JsonResult(new { success = false, message = "Invalid request." });
            }

            if (questionId <= 0)
                return new JsonResult(new { success = false, message = "Invalid question id." });

            try
            {
                // Ensure CurrentSurvey is loaded
                if (CurrentSurvey == null && surveyId > 0)
                {
                    try
                    {
                        CurrentSurvey = await _api.GetAsync<Models.Survey>($"v1/Surveys/Survey/{surveyId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load survey for SaveRestrictions: SurveyId={SurveyId}", surveyId);
                    }
                }

                if (CurrentSurvey == null || string.IsNullOrWhiteSpace(CurrentSurvey.SurveyCode))
                    return new JsonResult(new { success = false, message = "Survey not found." });

                // Load existing questions for the survey and find target
                var allQuestions = await _api.GetAsync<List<SurveyQuestionDto>>($"v1/SurveyQuestions/{CurrentSurvey.SurveyCode}") ?? new List<SurveyQuestionDto>();
                var target = allQuestions.FirstOrDefault(q => q.Id == questionId);
                if (target == null)
                    return new JsonResult(new { success = false, message = "Question not found." });

                // Convert incoming object back to canonical JSON string for storage
                string serialized = null;
                try
                {
                    serialized = System.Text.Json.JsonSerializer.Serialize(restrictions);
                }
                catch
                {
                    serialized = restrictions?.ToString();
                }

                target.Restriction = serialized;

                // Persist update via API update endpoint (existing pattern)
                await _api.PutJsonAsync<SurveyQuestionDto, object>($"v1/SurveyQuestions/update/{CurrentSurvey.SurveyCode}/{target.Id}", target);

                return new JsonResult(new
                {
                    success = true,
                    message = "Restrictions saved successfully."
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save restrictions for QuestionId={QuestionId}, SurveyId={SurveyId}", questionId, surveyId);
                return new JsonResult(new { success = false, message = "Failed to save restrictions." });
            }
        }


        // GET: lookup values by lookup id (used by skip logic)
        public async Task<JsonResult> OnGetLookupValuesAsync(int lookupId)
        {
            if (lookupId <= 0)
                return new JsonResult(new List<object>());

            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query payload on LookupValues GET");
                return new JsonResult(new List<object>());
            }

            try
            {
                var values = await _api.GetAsync<List<LookupTableValueDto>>(
                    $"v1/Lookups/values/by-lookup/{lookupId}"
                ) ?? new();

                // return only what the UI needs
                return new JsonResult(values.Select(v => new
                {
                    id = v.Id,
                    text = v.ItemName
                }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch lookup values for LookupId={LookupId}", lookupId);
                return new JsonResult(new List<object>());
            }
        }


        // POST handler: saves the calculated field JSON into ResultExpression for a single question (AJAX)
        public async Task<JsonResult> OnPostSaveCalculatedFieldAsync(int questionId, int surveyId, [FromBody] object calculatedFieldPayload)
        {
            if (Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync()
                //ContainsPathProbeInJsonPayload(calculatedFieldPayload)
                )
            {
                _logger.LogWarning("Blocked suspicious payload on SaveCalculatedField POST.");
                Response.StatusCode = 400;
                return new JsonResult(new { success = false, message = "Invalid request." });
            }

            if (questionId <= 0)
                return new JsonResult(new { success = false, message = "Invalid question id." });

            try
            {
                // Ensure CurrentSurvey is loaded (same pattern as other handlers)
                if (CurrentSurvey == null && surveyId > 0)
                {
                    try
                    {
                        CurrentSurvey = await _api.GetAsync<Models.Survey>($"v1/Surveys/Survey/{surveyId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load survey for SaveCalculatedField: SurveyId={SurveyId}", surveyId);
                    }
                }

                if (CurrentSurvey == null || string.IsNullOrWhiteSpace(CurrentSurvey.SurveyCode))
                    return new JsonResult(new { success = false, message = "Survey not found." });

                // Load existing questions for the survey and find target
                var allQuestions = await _api.GetAsync<List<SurveyQuestionDto>>($"v1/SurveyQuestions/{CurrentSurvey.SurveyCode}") ?? new List<SurveyQuestionDto>();
                var target = allQuestions.FirstOrDefault(q => q.Id == questionId);
                if (target == null)
                    return new JsonResult(new { success = false, message = "Question not found." });

                // Convert incoming object back to canonical JSON string for storage
                string serialized = null;
                try
                {
                    // Use System.Text.Json for canonical serialization (same approach as other handlers)
                    serialized = System.Text.Json.JsonSerializer.Serialize(calculatedFieldPayload);
                }
                catch
                {
                    // fallback - attempt ToString
                    serialized = calculatedFieldPayload?.ToString();
                }

                // Assign to ResultExpression (this is the field you requested)
                target.ResultExpression = serialized;

                // Persist update via API update endpoint
                await _api.PutJsonAsync<SurveyQuestionDto, object>($"v1/SurveyQuestions/update/{CurrentSurvey.SurveyCode}/{target.Id}", target);

                TempData["SuccessMessage"] = "Calculated field saved successfully.";

                // Return success (we follow restrictions pattern and ask caller to optionally reload)
                return new JsonResult(new { success = true, reload = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save calculated field for QuestionId={QuestionId}, SurveyId={SurveyId}", questionId, surveyId);
                return new JsonResult(new { success = false, message = "Failed to save calculated field." });
            }
        }



    }
}
