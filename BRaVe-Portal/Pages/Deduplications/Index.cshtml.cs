using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Deduplications
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IAppCache _cache;

        private readonly IRestApiService _api;

        public IndexModel(ILogger<IndexModel> logger, IAppCache cache, IRestApiService api)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
        }

    /*    [BindProperty(SupportsGet = true)]
        public List<Deduplication> Deduplications { get; private set; } = new();
        public List<Deduplication> FilteredUsers { get; set; } = new();

 

        public async Task OnGetAsync()
        {
            // Fetch all users every GET
            Deduplications = await _api.GetAsync<List<Deduplication>>("v1/ItemTags")
                 ?? new List<Deduplication>();

 

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                CurrentPage = 1; // reset page on search
                Deduplications = (List<Deduplication>)Deduplications
                    .Where(u => u.firstName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                     ).ToList();
            }

            TotalRecords = Deduplications.Count;

            // Apply paging
            FilteredUsers = Deduplications
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    */
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
  
     

       /* [BindProperty]
        public Deduplication UpdatedDeduplication { get; set; } = new();

        */
      


         
    }
}
