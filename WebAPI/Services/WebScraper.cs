using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using WebAPI.Models;
using System.Net;

namespace WebAPI.Services
{
    public class WebScraper
    {
        public List<WebData> Scrape(string url)
        {
            try
            {
                Console.WriteLine($"Starting to scrape URL: {url}");

                var web = new HtmlWeb();
                web.OverrideEncoding = Encoding.UTF8;
                web.AutoDetectEncoding = true;
                web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36";

                web.PreRequest = request => {
                    request.Timeout = 30000;
                    request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                    request.Headers.Add("Accept-Language", "en-US,en;q=0.9,tr;q=0.8");
                    request.Headers.Add("Cache-Control", "max-age=0");
                    request.Headers.Add("Sec-Fetch-Dest", "document");
                    request.Headers.Add("Sec-Fetch-Mode", "navigate");
                    request.Headers.Add("Sec-Fetch-Site", "none");
                    request.Headers.Add("Sec-Fetch-User", "?1");
                    request.Headers.Add("Upgrade-Insecure-Requests", "1");
                    request.Headers.Add("Referer", "https://www.google.com/");
                    return true;
                };

                Console.WriteLine("Loading page...");
                var doc = web.Load(url);

                string rawHtml = doc.DocumentNode.OuterHtml;
                Console.WriteLine($"Page loaded successfully. HTML length: {rawHtml.Length}");

                if (rawHtml.Length < 1000)
                {
                    Console.WriteLine("WARNING: Page appears to have very little content, may be blocked or redirected");
                    Console.WriteLine($"Page content preview: {rawHtml.Substring(0, Math.Min(rawHtml.Length, 500))}");
                }

                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                string pageTitle = titleNode?.InnerText.Trim() ?? "No Title";
                Console.WriteLine($"Page title found: {pageTitle}");

                var articles = new List<WebData>();
                var timestamp = DateTime.UtcNow;

                if (url.Contains("sozcu.com.tr"))
                {
                    Console.WriteLine("Using custom extraction for sozcu.com.tr");

                    Console.WriteLine("Looking for news-card elements...");
                    var newsCards = doc.DocumentNode.SelectNodes("//div[contains(@class, 'news-card')]");
                    if (newsCards != null)
                    {
                        Console.WriteLine($"Found {newsCards.Count} news cards");
                        foreach (var card in newsCards)
                        {
                            Console.WriteLine($"Processing news card: {card.OuterHtml.Substring(0, Math.Min(card.OuterHtml.Length, 100))}...");
                            try
                            {
                                var contentBuilder = new StringBuilder();
                                string title = "News Article";
                                string articleUrl = url;

                                var allLinks = card.SelectNodes(".//a[@href]");
                                if (allLinks != null)
                                {
                                    Console.WriteLine($"Found {allLinks.Count} links in card");
                                    foreach (var link in allLinks)
                                    {
                                        Console.WriteLine($"Link: {link.OuterHtml}");

                                        if (link.Attributes["href"] != null)
                                        {
                                            articleUrl = link.Attributes["href"].Value;
                                            if (!articleUrl.StartsWith("http"))
                                            {
                                                articleUrl = $"https://www.sozcu.com.tr{articleUrl}";
                                            }
                                        }

                                        string linkText = link.InnerText.Trim();
                                        if (!string.IsNullOrWhiteSpace(linkText) && linkText.Length > 5 && title == "News Article")
                                        {
                                            title = linkText;
                                            contentBuilder.AppendLine(linkText);
                                        }
                                    }
                                }

                                var images = card.SelectNodes(".//img");
                                if (images != null)
                                {
                                    Console.WriteLine($"Found {images.Count} images in card");
                                    foreach (var image in images)
                                    {
                                        if (image.Attributes["alt"] != null)
                                        {
                                            string altText = image.Attributes["alt"].Value.Trim();
                                            if (!string.IsNullOrWhiteSpace(altText))
                                            {
                                                if (title == "News Article")
                                                {
                                                    title = altText;
                                                }
                                                contentBuilder.AppendLine(altText);
                                                Console.WriteLine($"Added alt text: {altText}");
                                            }
                                        }
                                    }
                                }

                                if (contentBuilder.Length > 0 && !string.IsNullOrWhiteSpace(title) && title != "News Article")
                                {
                                    string formattedContent = $"{title}|{contentBuilder.ToString().Replace(Environment.NewLine, " ")}";

                                    articles.Add(new WebData
                                    {
                                        Url = articleUrl,
                                        Title = title,
                                        Content = formattedContent,
                                        CrawledAt = timestamp
                                    });
                                    Console.WriteLine($"Added article: {title}");
                                }
                            }
                            catch (Exception cardEx)
                            {
                                Console.WriteLine($"Error processing card: {cardEx.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No news-card elements found");
                    }

                    if (articles.Count == 0)
                    {
                        Console.WriteLine("No news cards found, trying alternative approach...");
                        var potentialArticleDivs = doc.DocumentNode.SelectNodes("//div[contains(@class, 'col-')][.//a[@href] and .//img]");
                        if (potentialArticleDivs != null)
                        {
                            Console.WriteLine($"Found {potentialArticleDivs.Count} potential article divs");
                            foreach (var div in potentialArticleDivs)
                            {
                                try
                                {
                                    var contentBuilder = new StringBuilder();
                                    string title = "Article";
                                    string articleUrl = url;

                                    var link = div.SelectSingleNode(".//a[@href]");
                                    if (link != null && link.Attributes["href"] != null)
                                    {
                                        articleUrl = link.Attributes["href"].Value;
                                        if (!articleUrl.StartsWith("http"))
                                        {
                                            articleUrl = $"https://www.sozcu.com.tr{articleUrl}";
                                        }

                                        string linkText = link.InnerText.Trim();
                                        if (!string.IsNullOrWhiteSpace(linkText) && linkText.Length > 5)
                                        {
                                            title = linkText;
                                            contentBuilder.AppendLine(linkText);
                                        }
                                    }

                                    var image = div.SelectSingleNode(".//img");
                                    if (image != null && image.Attributes["alt"] != null)
                                    {
                                        string altText = image.Attributes["alt"].Value.Trim();
                                        if (!string.IsNullOrWhiteSpace(altText))
                                        {
                                            if (title == "Article") title = altText;
                                            contentBuilder.AppendLine(altText);
                                        }
                                    }

                                    if (contentBuilder.Length > 0 && !string.IsNullOrWhiteSpace(title) && title != "Article")
                                    {
                                        string formattedContent = $"{title}|{contentBuilder.ToString().Replace(Environment.NewLine, " ")}";

                                        articles.Add(new WebData
                                        {
                                            Url = articleUrl,
                                            Title = title,
                                            Content = formattedContent,
                                            CrawledAt = timestamp
                                        });
                                        Console.WriteLine($"Added alternative article: {title}");
                                    }
                                }
                                catch (Exception divEx)
                                {
                                    Console.WriteLine($"Error processing div: {divEx.Message}");
                                }
                            }
                        }
                    }

                    if (articles.Count == 0)
                    {
                        Console.WriteLine("Still no articles found, trying last resort methods...");

                        var allPossibleLinks = doc.DocumentNode.SelectNodes("//a[@href]");
                        if (allPossibleLinks != null)
                        {
                            Console.WriteLine($"Found {allPossibleLinks.Count} links total on page");
                            foreach (var link in allPossibleLinks)
                            {
                                try
                                {
                                    string href = link.Attributes["href"]?.Value?.Trim() ?? "";
                                    if (href.Contains("-p") || href.Contains("/gundem/") || href.Contains("/ekonomi/"))
                                    {
                                        string linkText = link.InnerText.Trim();
                                        if (linkText.Length > 10)
                                        {
                                            string articleUrl = href;
                                            if (!articleUrl.StartsWith("http"))
                                            {
                                                articleUrl = $"https://www.sozcu.com.tr{articleUrl}";
                                            }

                                            string formattedContent = $"{linkText}|{linkText}";

                                            articles.Add(new WebData
                                            {
                                                Url = articleUrl,
                                                Title = linkText,
                                                Content = formattedContent,
                                                CrawledAt = timestamp
                                            });
                                            Console.WriteLine($"Added last resort article: {linkText}");
                                        }
                                    }
                                }
                                catch (Exception linkEx)
                                {
                                    Console.WriteLine($"Error processing link: {linkEx.Message}");
                                }
                            }
                        }
                    }
                }

                if (articles.Count == 0)
                {
                    Console.WriteLine("No specific articles found, adding page as a single article");
                    var contentBuilder = new StringBuilder();
                    int elementsFound = 0;

                    var metaDescription = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
                    if (metaDescription != null && metaDescription.Attributes["content"] != null)
                    {
                        contentBuilder.AppendLine(metaDescription.Attributes["content"].Value);
                        elementsFound++;
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
                    }

                    if (contentBuilder.Length == 0)
                    {
                        contentBuilder.AppendLine(pageTitle);
                        elementsFound++;
                    }

                    string content = CleanContent(contentBuilder.ToString());

                    string formattedContent = $"{pageTitle}|{content}";

                    articles.Add(new WebData
                    {
                        Url = url,
                        Title = pageTitle,
                        Content = formattedContent,
                        CrawledAt = timestamp
                    });

                    Console.WriteLine($"Added page as a single article with {elementsFound} elements");
                }

                if (articles.Count > 1)
                {
                    var combinedArticleContents = new StringBuilder();
                    foreach (var article in articles)
                    {
                        if (combinedArticleContents.Length > 0)
                        {
                            combinedArticleContents.Append("||");
                        }
                        combinedArticleContents.Append(article.Content);
                    }

                    return new List<WebData>
                    {
                        new WebData
                        {
                            Url = url,
                            Title = pageTitle,
                            Content = combinedArticleContents.ToString(),
                            CrawledAt = timestamp
                        }
                    };
                }

                Console.WriteLine($"Total articles extracted: {articles.Count}");
                return articles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping {url}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return new List<WebData>
                {
                    new WebData
                    {
                        Url = url,
                        Title = "Error crawling page",
                        Content = $"Error: {ex.Message}",
                        CrawledAt = DateTime.UtcNow
                    }
                };
            }
        }

        private string CleanContent(string content)
        {
            string cleaned = content.Trim();
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            cleaned = Regex.Replace(cleaned, @"[\p{C}]", string.Empty);
            return cleaned;
        }
    }
}