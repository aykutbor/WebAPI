using RestSharp;
using System.Text.Json;
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
            try
            {
                var request = new RestRequest("", Method.Post);
                request.AddJsonBody(new { url }); // JSON formatýnda bir nesne gönderiyoruz
                var response = await _client.ExecuteAsync(request);
                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    return JsonSerializer.Deserialize<WebData>(response.Content);
                }
                // Hata durumunda yanýtý kontrol edin
                Console.WriteLine($"Error: {response.StatusCode}, Content: {response.Content}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CrawlAsync: {ex.Message}");
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
                // Log the exception
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
                // Log the exception
                Console.WriteLine($"Error in GetLatestAsync: {ex.Message}");
                return new List<WebData>();
            }
        }
    }
} 