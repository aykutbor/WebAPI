namespace WebCrawlerUI.Models
{
    public class WebData
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CrawledAt { get; set; }
        public List<NewsItem> NewsItems { get; set; } = new List<NewsItem>();

        public string CrawledAtLocal => CrawledAt.AddHours(3).ToString("dd.MM.yyyy HH:mm");
    }

    public class NewsItem
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}