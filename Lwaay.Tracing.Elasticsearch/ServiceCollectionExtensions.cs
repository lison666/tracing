using Lwaay.Tracing.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Elasticsearch
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTracingStorageToElasticSearch(this IServiceCollection serviceDescriptors,
            Action<ElasticsearchOption> action)
        {
            return serviceDescriptors
                .AddOptions().Configure<ElasticsearchOption>(action)
                .AddSingleton<IElasticClientFactory, ElasticClientFactory>()
                .AddSingleton<IServiceQuery, ServcieOperationService>()
                 .AddSingleton<IServcieOperationStorage, ServcieOperationService>()
                 .AddSingleton<ITracingStorage, SpanService>()
                 .AddSingleton<ITracingQuery, SpanService>();
        }
    }
}
