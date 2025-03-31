using Microsoft.AspNetCore.Mvc;
using WebAPI.Models;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrawlController : ControllerBase
    {
        private readonly WebScraper _scraper;
        private readonly ElasticsearchService _elasticsearch;

        public CrawlController(WebScraper scraper, ElasticsearchService elasticsearch)
        {
            _scraper = scraper;
            _elasticsearch = elasticsearch;
        }

        [HttpPost]
        public IActionResult CrawlAndSave([FromBody] UrlRequest request)
        {
            if (string.IsNullOrEmpty(request.Url))
            {
                return BadRequest("URL cannot be empty");
            }

            var dataList = _scraper.Scrape(request.Url);
            var successCount = 0;

            foreach (var data in dataList)
            {
                if (_elasticsearch.IndexDocument(data))
                {
                    successCount++;
                }
            }

            if (successCount == 0 && dataList.Count > 0)
            {
                return StatusCode(500, "Failed to index any documents in Elasticsearch");
            }

            return Ok(new
            {
                TotalArticles = dataList.Count,
                SuccessfullyIndexed = successCount,
                Articles = dataList
            });
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string query, [FromQuery] int take = 10)
        {
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Search query cannot be empty");
            }

            var results = _elasticsearch.Search(query, take);
            return Ok(results);
        }


        [HttpGet("latest")]
        public IActionResult GetLatest([FromQuery] int take = 10)
        {
            var results = _elasticsearch.GetLatest(take);
            return Ok(results);
        }
    }
}