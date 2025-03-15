namespace WebCrawlerUI.Models
{
    public class WebData
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CrawledAt { get; set; }
    }
} 