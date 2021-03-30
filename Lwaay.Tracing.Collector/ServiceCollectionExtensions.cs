using Lwaay.Tracing.Core.InMeoryPipeline;
using Lwaay.Tracing.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Grpc
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCollector(this IServiceCollection serviceDescriptors)
        {
            return serviceDescriptors.AddInMeoryTracingPipeline(opt =>
            {
                opt.MaxHanderParallelism = 1;
                opt.BatchProcessSize = 1;
                opt.BatchSpanSize = 1;
            }).AddTracingStorageToElasticSearch(opt =>
            {
                opt.Host = "http://localhost:9200/";
                opt.IndexRefreshInterrval = 1;
                opt.TracingIndexNumberOfShards = 1;
                opt.TracingNumberOfReplicas = 1;
            });
        }
    }
}
