using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebCrawlerUI.Models;
using WebCrawlerUI.Services;

namespace WebCrawlerUI.Pages
{
    public class LatestModel : PageModel
    {
        private readonly ILogger<LatestModel> _logger;
        private readonly ApiService _apiService;

        public LatestModel(ILogger<LatestModel> logger, ApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        public List<WebData> LatestPages { get; set; } = new List<WebData>();
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving latest pages...");
                var fetchedPages = await _apiService.GetLatestAsync();
                _logger.LogInformation($"Retrieved {fetchedPages.Count} latest pages");
                // Create a HashSet to track unique URLs
                HashSet<string> uniqueUrls = new HashSet<string>();
                // Filter out duplicates based on URL
                LatestPages = fetchedPages
                    .GroupBy(p => p.Url)
                    .Select(g => g.First())
                    .ToList();
                _logger.LogInformation($"After filtering duplicates: {LatestPages.Count} unique pages");
                foreach (var page in LatestPages)
                {
                    _logger.LogInformation($"Latest Page: Id={page.Id}, Title={page.Title}, URL={page.Url}, Content Length={page.Content?.Length ?? 0}");
                    // Also deduplicate news items within each page
                    if (page.NewsItems?.Any() == true)
                    {
                        page.NewsItems = page.NewsItems
                            .GroupBy(n => n.Title)
                            .Select(g => g.First())
                            .ToList();
                    }
                }
                if (LatestPages.Count == 0)
                {
                    ErrorMessage = "No crawled pages found. Try crawling some pages first.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest pages");
                ErrorMessage = $"Error retrieving latest pages: {ex.Message}";
            }
            return Page();
        }
    }
}