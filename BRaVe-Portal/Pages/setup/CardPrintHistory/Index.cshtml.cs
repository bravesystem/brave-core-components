using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;

public class PrintedCardsModel : PageModel
{
    private readonly ILogger<PrintedCardsModel> _logger;
    private readonly IRestApiService _api;

    public PrintedCardsModel(
        ILogger<PrintedCardsModel> logger,
        IRestApiService api)
    {
        _logger = logger;
        _api = api;
    }

    // ------------------ DATA ------------------

    [BindProperty(SupportsGet = true)]
    public List<PrintedCardSummaryDto> Records { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty]
    public int CardId { get; set; }

    [BindProperty]
    public string Reason { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public int TotalPages { get; set; }

    // ------------------ LOAD ------------------

    public async Task OnGetAsync()
    {
        _logger.LogInformation("Loading printed card records");

        try
        {
            var url = "v1/card-printing/printed-records?";

            if (StartDate.HasValue)
                url += $"startDate={StartDate:yyyy-MM-dd}&";

            if (EndDate.HasValue)
                url += $"endDate={EndDate:yyyy-MM-dd}";

            Records = await _api.GetAsync<List<PrintedCardSummaryDto>>(url) ?? new();

            _logger.LogInformation("Loaded {Count} printed records", Records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading printed records");
            Records = new();
            TempData["ErrorMessage"] = "Failed to load records.";
        }


        var totalItems = Records.Count;

        TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

        Records = Records
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    // ------------------ AUTHORIZE REPRINT ------------------

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAuthorizeReprintAsync()
    {
        if (CardId <= 0 || string.IsNullOrWhiteSpace(Reason))
        {
            TempData["ErrorMessage"] = "Invalid request.";
            return RedirectToPage();
        }

        try
        {
            var payload = new
            {
                cardId = CardId,
                reason = Reason
            };

            await _api.PostJsonAsync<object, object>(
                "v1/card-printing/authorize-reprint",
                payload
            );

            TempData["SuccessMessage"] = "Reprint authorized successfully.";
            _logger.LogInformation("Reprint authorized for CardId {CardId}", CardId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authorize reprint for CardId {CardId}", CardId);
            TempData["ErrorMessage"] = "Failed to  reprint. Please try again";
        }

        return RedirectToPage();
    }
}