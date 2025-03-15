namespace WebAPI.Models
{
    public class WebData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CrawledAt { get; set; } = DateTime.UtcNow;

    }

    public class UrlRequest
    {
        public string Url { get; set; }
    }
} 