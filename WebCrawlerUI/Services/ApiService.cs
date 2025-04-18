using RestSharp;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebCrawlerUI.Models;

namespace WebCrawlerUI.Services
{
    public class ApiService
    {
        private readonly RestClient _client;

        public ApiService(IConfiguration configuration)
        {
            //string apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/api/crawl";
            string apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://host.docker.internal:5001/api/crawl";
            _client = new RestClient(apiBaseUrl);
        }

        public async Task<WebData?> CrawlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("{\"error\": \"URL cannot be null or empty.\"}");
                return null;
            }

            var request = new RestRequest("", Method.Post)
                .AddHeader("Content-Type", "application/json")
                .AddJsonBody(new { url });

            RestResponse? response = null;

            try
            {
                response = await _client.ExecuteAsync(request);
                Console.WriteLine($"Crawl Response: Status={response.StatusCode}, Content Length={response.Content?.Length ?? 0}");

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<CrawlResponse>(response.Content, options);

                        if (apiResponse != null && apiResponse.Articles != null && apiResponse.Articles.Count > 0)
                        {
                            var firstArticle = apiResponse.Articles[0];
                            Console.WriteLine($"Successfully parsed API response with {apiResponse.Articles.Count} articles");

                            return firstArticle;
                        }
                    }
                    catch (JsonException)
                    {
                        Console.WriteLine("Could not parse as CrawlResponse, trying as WebData");
                        try
                        {
                            var webData = JsonSerializer.Deserialize<WebData>(response.Content, options);
                            if (webData != null)
                            {
                                Console.WriteLine($"Successfully parsed as single WebData: Title={webData.Title}, Content Length={webData.Content?.Length ?? 0}");
                                return webData;
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"Error parsing as WebData: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    error = "CrawlAsync failed",
                    statusCode = response.StatusCode,
                    content = response.Content?.Substring(0, Math.Min(100, response.Content?.Length ?? 0)) ?? "No content"
                }));

                return null;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    error = "HTTP request error",
                    message = ex.Message,
                    url
                }));
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    error = "Unexpected error",
                    message = ex.Message,
                    content = response?.Content?.Substring(0, Math.Min(100, response?.Content?.Length ?? 0)) ?? "No content"
                }));
                return null;
            }
        }

        public async Task<List<WebData>> SearchAsync(string query)
        {
            try
            {
                var request = new RestRequest($"search?query={query}", Method.Get);
                var response = await _client.ExecuteAsync(request);

                Console.WriteLine($"Search Response: Status={response.StatusCode}, Content={response.Content?.Substring(0, Math.Min(100, response.Content?.Length ?? 0))}...");

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };
                    var results = JsonSerializer.Deserialize<List<WebData>>(response.Content, options) ?? new List<WebData>();

                    foreach (var result in results)
                    {
                        Console.WriteLine($"Search Result: Id={result.Id}, Title={result.Title}, Url={result.Url}, Content Length={result.Content?.Length ?? 0}");

                        if (result.Content == null)
                        {
                            result.Content = "";
                        }

                        if (result.NewsItems != null && result.NewsItems.Any())
                        {
                            bool hasRelevantNewsItems = false;

                            var relevantNewsItems = result.NewsItems
                                .Where(item =>
                                    (item.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                                    (item.Content?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
                                .ToList();

                            if (relevantNewsItems.Any())
                            {
                                result.NewsItems = relevantNewsItems;
                                hasRelevantNewsItems = true;
                            }

                            Console.WriteLine($"Found {relevantNewsItems.Count} relevant news items out of {result.NewsItems.Count}");
                        }
                    }

                    return results;
                }
                else
                {
                    Console.WriteLine($"Search failed: Status={response.StatusCode}, Content={response.Content ?? "No content"}");
                }

                return new List<WebData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchAsync: {ex.Message}");
                return new List<WebData>();
            }
        }

        public async Task<List<WebData>> GetLatestAsync(int take = 10)
        {
            try
            {
                var request = new RestRequest("latest", Method.Get)
                    .AddQueryParameter("take", take.ToString());
                var response = await _client.ExecuteAsync(request);
                Console.WriteLine($"Latest Response: Status={response.StatusCode}, Content={response.Content?.Substring(0, Math.Min(100, response.Content?.Length ?? 0))}...");
                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };
                    var results = JsonSerializer.Deserialize<List<WebData>>(response.Content, options) ?? new List<WebData>();

                    var uniqueResults = results
                        .GroupBy(r => r.Url)
                        .Select(g => g.First())
                        .ToList();

                    Console.WriteLine($"API returned {results.Count} results, filtered to {uniqueResults.Count} unique pages");

                    foreach (var result in uniqueResults)
                    {
                        Console.WriteLine($"Latest Result: Id={result.Id}, Title={result.Title}, Url={result.Url}, Content Length={result.Content?.Length ?? 0}");
                        if (result.Content == null)
                        {
                            result.Content = "";
                        }

                        if (result.NewsItems?.Any() == true)
                        {
                            var uniqueNewsItems = result.NewsItems
                                .GroupBy(n => n.Title)
                                .Select(g => g.First())
                                .ToList();

                            result.NewsItems = uniqueNewsItems;
                            Console.WriteLine($"Page {result.Title}: Filtered from {result.NewsItems.Count} to {uniqueNewsItems.Count} unique news items");
                        }
                    }
                    return uniqueResults;
                }
                else
                {
                    Console.WriteLine($"Latest failed: Status={response.StatusCode}, Content={response.Content ?? "No content"}");
                }
                return new List<WebData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLatestAsync: {ex.Message}");
                return new List<WebData>();
            }
        }
    }

    internal class CrawlResponse
    {
        public int TotalArticles { get; set; }
        public int SuccessfullyIndexed { get; set; }
        public List<WebData> Articles { get; set; } = new List<WebData>();
    }
}