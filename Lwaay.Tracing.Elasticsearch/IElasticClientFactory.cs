using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lwaay.Tracing.Elasticsearch
{
    interface IElasticClientFactory
    {
        ElasticClient Get();
    }

    class ElasticClientFactory : IElasticClientFactory
    {
        ElasticsearchOption ElasticsearchOption;
        ILogger<ElasticClientFactory> Logger;
        private readonly Lazy<ElasticClient> _value;

        public ElasticClientFactory(IOptions<ElasticsearchOption> options,ILogger<ElasticClientFactory> logger)
        {
            ElasticsearchOption = options.Value;
            Logger = logger;
            _value = new Lazy<ElasticClient>(() => CreatElasticClient(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }
        private ElasticClient CreatElasticClient()
        {
            try
            {
                var urls = ElasticsearchOption.Host.Split(';').Select(x => new Uri(x)).ToArray();
                Logger.LogInformation($"Init ElasticClient with options: ElasticSearchHosts={ElasticsearchOption.Host}.");
                var pool = new StaticConnectionPool(urls);
                var settings = new ConnectionSettings(pool);
                settings.EnableDebugMode(c =>
                {

                });
                var client = new ElasticClient(settings);
                return client;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Create ElasticClient failed.");
                throw;
            }

        }
        public ElasticClient Get()
        {
            return _value.Value;
        }
    }

}
