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

            var data = _scraper.Scrape(request.Url);
            var success = _elasticsearch.IndexDocument(data);

            if (!success)
            {
                return StatusCode(500, "Failed to index document in Elasticsearch");
            }

            return Ok(data);
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