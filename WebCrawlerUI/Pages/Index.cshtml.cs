using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebCrawlerUI.Models;
using WebCrawlerUI.Services;

namespace WebCrawlerUI.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ApiService _apiService;

    public IndexModel(ILogger<IndexModel> logger, ApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }

    [BindProperty]
    public string UrlToCrawl { get; set; } = string.Empty;
    
    [BindProperty(SupportsGet = true)]
    public string Query { get; set; } = string.Empty;
    
    public WebData? CrawledData { get; set; }
    public List<WebData> SearchResults { get; set; } = new List<WebData>();
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrEmpty(Query))
        {
            try
            {
                SearchResults = await _apiService.SearchAsync(Query);
                if (SearchResults.Count == 0)
                {
                    ErrorMessage = "No results found for your search query.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for {Query}", Query);
                ErrorMessage = $"Error searching: {ex.Message}";
            }
        }
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(UrlToCrawl))
        {
            ErrorMessage = "URL cannot be empty";
            return Page();
        }

        try
        {
            CrawledData = await _apiService.CrawlAsync(UrlToCrawl);
            if (CrawledData == null)
            {
                ErrorMessage = "Failed to crawl the URL. Please check if the URL is valid and try again.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crawling {Url}", UrlToCrawl);
            ErrorMessage = $"Error crawling: {ex.Message}";
        }
        
        return Page();
    }
}
