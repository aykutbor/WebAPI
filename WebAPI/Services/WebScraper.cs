using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using WebAPI.Models;

namespace WebAPI.Services
{
    public class WebScraper
    {
        public WebData Scrape(string url)
        {
            try
            {
                Console.WriteLine($"Starting to scrape URL: {url}");
                var web = new HtmlWeb();
                web.OverrideEncoding = Encoding.UTF8; 
                web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36"; 

                var doc = web.Load(url);
                Console.WriteLine("Page loaded successfully");
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                string title = titleNode?.InnerText.Trim() ?? "No Title";
                Console.WriteLine($"Title found: {title}");

                var contentBuilder = new StringBuilder();
                int elementsFound = 0;

                var metaDescription = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
                if (metaDescription != null && metaDescription.Attributes["content"] != null)
                {
                    contentBuilder.AppendLine(metaDescription.Attributes["content"].Value);
                    elementsFound++;
                    Console.WriteLine("Found meta description");
                }

                var headings = doc.DocumentNode.SelectNodes("//h1|//h2|//h3");
                if (headings != null)
                {
                    foreach (var heading in headings)
                    {
                        contentBuilder.AppendLine(heading.InnerText.Trim());
                        elementsFound++;
                    }
                    Console.WriteLine($"Found {headings.Count} headings");
                }

                var paragraphs = doc.DocumentNode.SelectNodes("//p");
                if (paragraphs != null)
                {
                    foreach (var p in paragraphs)
                    {

                        string text = p.InnerText.Trim();
                        if (text.Length > 10)
                        {
                            contentBuilder.AppendLine(text);
                            elementsFound++;
                        }
                    }
                    Console.WriteLine($"Found {paragraphs.Count} paragraphs");
                }

                var articleContent = doc.DocumentNode.SelectNodes("//article");
                if (articleContent != null)
                {
                    foreach (var article in articleContent)
                    {
                        contentBuilder.AppendLine(article.InnerText.Trim());
                        elementsFound++;
                    }
                    Console.WriteLine($"Found {articleContent.Count} article elements");
                }

                var contentDivs = doc.DocumentNode.SelectNodes("//div[contains(@class, 'content') or contains(@class, 'article') or contains(@class, 'news') or contains(@class, 'text') or contains(@class, 'body') or contains(@class, 'main')]");
                if (contentDivs != null)
                {
                    foreach (var div in contentDivs)
                    {

                        string text = div.InnerText.Trim();
                        if (text.Length > 50)
                        {
                            contentBuilder.AppendLine(text);
                            elementsFound++;
                        }
                    }
                    Console.WriteLine($"Found {contentDivs.Count} content divs");
                }

                if (url.Contains("sozcu.com.tr"))
                {
                    Console.WriteLine("Detected sozcu.com.tr, trying specific selectors");

                    var mainContent = doc.DocumentNode.SelectSingleNode("//div[@class='main-content']");
                    if (mainContent != null)
                    {
                        contentBuilder.AppendLine(mainContent.InnerText.Trim());
                        elementsFound++;
                        Console.WriteLine("Found main-content div");
                    }

                    var newsDetail = doc.DocumentNode.SelectSingleNode("//div[@class='news-detail']");
                    if (newsDetail != null)
                    {
                        contentBuilder.AppendLine(newsDetail.InnerText.Trim());
                        elementsFound++;
                        Console.WriteLine("Found news-detail div");
                    }

                    var newsContent = doc.DocumentNode.SelectSingleNode("//div[@class='news-content']");
                    if (newsContent != null)
                    {
                        contentBuilder.AppendLine(newsContent.InnerText.Trim());
                        elementsFound++;
                        Console.WriteLine("Found news-content div");
                    }
                }

                if (contentBuilder.Length == 0)
                {
                    Console.WriteLine("No specific content found, falling back to body");
                    var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
                    if (bodyNode != null)
                    {
                        contentBuilder.AppendLine(bodyNode.InnerText.Trim());
                        elementsFound++;
                    }
                }
                string content = contentBuilder.ToString().Trim();

                content = Regex.Replace(content, @"\s+", " ");
                content = Regex.Replace(content, @"[\p{C}]", string.Empty);

                Console.WriteLine($"Scraped content length: {content.Length} characters from {elementsFound} elements");
                Console.WriteLine($"Content preview: {(content.Length > 100 ? content.Substring(0, 100) + "..." : content)}");
                return new WebData
                {
                    Url = url,
                    Title = title,
                    Content = content,
                    CrawledAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping {url}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return new WebData
                {
                    Url = url,
                    Title = "Error crawling page",
                    Content = $"Error: {ex.Message}",
                    CrawledAt = DateTime.UtcNow
                };
            }
        }
    }
}