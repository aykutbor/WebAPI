using Nest;
using WebAPI.Models;

namespace WebAPI.Services
{
    public class ElasticsearchService
    {
        private readonly ElasticClient _client;

        public ElasticsearchService(string elasticsearchUrl = "http://localhost:9200")
        {
            var settings = new ConnectionSettings(new Uri(elasticsearchUrl))
                .DefaultIndex("webdata");

            _client = new ElasticClient(settings);

            if (!_client.Indices.Exists("webdata").Exists)
            {
                var createIndexResponse = _client.Indices.Create("webdata", c => c
                    .Map<WebData>(m => m
                        .AutoMap()
                        .Properties(p => p
                            .Text(t => t.Name(n => n.Content).Analyzer("standard"))
                            .Text(t => t.Name(n => n.Title).Analyzer("standard"))
                        )
                    )
                );
            }
        }

        public bool IndexDocument(WebData data)
        {
            var response = _client.IndexDocument(data);
            return response.IsValid;
        }

        public IReadOnlyCollection<WebData> Search(string query, int take = 10)
        {
            var response = _client.Search<WebData>(s => s
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Fields(f => f
                            .Field(p => p.Title, 2.0)
                            .Field(p => p.Content)
                        )
                        .Query(query)
                        .Type(TextQueryType.BestFields)
                        .Fuzziness(Fuzziness.Auto)
                    )
                )
                .Size(take)
            );

            return response.Documents;
        }

        public IReadOnlyCollection<WebData> GetLatest(int take = 10)
        {
            // First, get unique URLs by selecting the most recent entry for each URL
            var uniqueUrlResponse = _client.Search<WebData>(s => s
                .Size(0)
                .Aggregations(a => a
                    .Terms("urls", t => t
                        .Field("url.keyword")
                        .Size(take * 2)
                        .Order(o => o
                            .Descending("latestDate")
                        )
                        .Aggregations(aa => aa
                            .Max("latestDate", m => m
                                .Field(f => f.CrawledAt)
                            )
                        )
                    )
                )
            );

            // Extract the list of unique URLs
            var uniqueUrls = uniqueUrlResponse.Aggregations.Terms("urls")
                .Buckets
                .Select(b => b.Key)
                .ToList();

            if (uniqueUrls.Count == 0)
            {
                // Fallback to original method if no aggregation results
                var fallbackResponse = _client.Search<WebData>(s => s
                    .Sort(sort => sort
                        .Descending(d => d.CrawledAt)
                    )
                    .Size(take)
                );
                return fallbackResponse.Documents;
            }

            // Now get the actual documents for these URLs, most recent first
            var response = _client.Search<WebData>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Filter(f => f
                            .Terms(t => t
                                .Field("url.keyword")
                                .Terms(uniqueUrls.Take(take))
                            )
                        )
                    )
                )
                .Sort(sort => sort
                    .Descending(d => d.CrawledAt)
                )
                .Size(take)
            );

            return response.Documents;
        }
    }
}