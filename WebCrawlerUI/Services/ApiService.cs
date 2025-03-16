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

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    return JsonSerializer.Deserialize<WebData>(response.Content, options);
                }

                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    error = "CrawlAsync failed",
                    statusCode = response.StatusCode,
                    content = response.Content ?? "No content"
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
            catch (JsonException ex)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    error = "JSON deserialization error",
                    message = ex.Message,
                    content = response?.Content ?? "No content" 
                }));
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    error = "Unexpected error",
                    message = ex.Message
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

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    return JsonSerializer.Deserialize<List<WebData>>(response.Content) ?? new List<WebData>();
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
                var request = new RestRequest($"latest?take={take}", Method.Get);
                var response = await _client.ExecuteAsync(request);
                
                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    return JsonSerializer.Deserialize<List<WebData>>(response.Content) ?? new List<WebData>();
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
} 