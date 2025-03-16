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
            var response = _client.Search<WebData>(s => s
                .Sort(sort => sort
                    .Descending(d => d.CrawledAt)
                )
                .Size(take)
            );

            return response.Documents;
        }
    }
} 