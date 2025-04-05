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
                LatestPages = await _apiService.GetLatestAsync(50);

                LatestPages = LatestPages
                    .OrderByDescending(p => p.CrawledAt)
                    .Take(20)
                    .ToList();

                foreach (var page in LatestPages)
                {
                    if (page.NewsItems != null && page.NewsItems.Any())
                    {
                        var importantItems = page.NewsItems
                            .Where(n => !string.IsNullOrWhiteSpace(n.Content) && n.Content.Length > 100)
                            .OrderByDescending(n => n.Content?.Length ?? 0)
                            .Take(10);

                        var randomItems = page.NewsItems
                            .OrderBy(x => Guid.NewGuid())
                            .Take(10);

                        page.NewsItems = importantItems.Union(randomItems)
                            .OrderByDescending(n => n.Content?.Length ?? 0)
                            .ToList();

                        foreach (var item in page.NewsItems)
                        {
                            if (!string.IsNullOrEmpty(item.Content))
                            {
                                item.Content = item.Content.TrimStart();
                            }
                        }
                    }
                }

                _logger.LogInformation($"Retrieved {LatestPages.Count} latest pages");
                foreach (var page in LatestPages)
                {
                    _logger.LogInformation($"Latest Page: Id={page.Id}, Title={page.Title}, URL={page.Url}, Content Length={page.Content?.Length ?? 0}, News Items: {page.NewsItems?.Count ?? 0}");
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