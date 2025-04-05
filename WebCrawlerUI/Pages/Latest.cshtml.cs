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
                LatestPages = await _apiService.GetLatestAsync(20);

                LatestPages = LatestPages
                    .GroupBy(p => p.Title)
                    .Select(g => g.First())
                    .Take(10)
                    .ToList();

                _logger.LogInformation($"Retrieved {LatestPages.Count} latest pages");
                foreach (var page in LatestPages)
                {
                    _logger.LogInformation($"Latest Page: Id={page.Id}, Title={page.Title}, URL={page.Url}, Content Length={page.Content?.Length ?? 0}");
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