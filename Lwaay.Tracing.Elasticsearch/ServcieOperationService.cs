using Lwaay.Tracing.Core;
using Lwaay.Tracing.Elasticsearch.Data;
using Lwaay.Tracing.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Elasticsearch
{
    class ServcieOperationService : IServiceQuery, IServcieOperationStorage
    {
        IMemoryCache MemoryCache;
        ElasticClient ElasticClient;
        ILogger<ServcieOperationService> Logger;
        private static IndexName _IndexName = "serviceoperationmodel";//IndexName.From<ServiceOperationModel>();
        public ServcieOperationService(IMemoryCache memoryCache,
            IElasticClientFactory elasticClientFactory,
            ILogger<ServcieOperationService> logger)
        {
            MemoryCache = memoryCache;
            ElasticClient = elasticClientFactory.Get();
            Logger = logger;
            TryCreateIndex();
        }

        private void TryCreateIndex()
        {
            if (!ElasticClient.Indices.Exists(_IndexName).Exists)
            {
                var serviceOperation = new CreateIndexDescriptor(_IndexName);
                serviceOperation.Map<ServiceOperationModel>(m => m.AutoMap()
                .Properties(p => p.Keyword(k => k.Name(c => c.Service)))
                .Properties(p => p.Keyword(k => k.Name(c => c.Operation))));
                var r= ElasticClient.Indices.Create(serviceOperation);
            }
        }


        public async Task<IEnumerable<string>> GetOperation(string serviceName, CancellationToken cancellationToken)
        {
            var result = await ElasticClient.SearchAsync<ServiceOperationModel>(s =>s.Index(_IndexName).Query(q => q.Term(t => t.Service, serviceName)));
            return result.Documents.Select(s => s.Operation);
        }

        public async Task<IEnumerable<string>> GetServices(CancellationToken cancellationToken)
        {
            var result = await ElasticClient.SearchAsync<ServiceOperationModel>(s => s.Index(_IndexName).MatchAll());
            return result.Documents.Select(s => s.Service);
        }
        private string CreateCacheKey(string serviceName,string operation)
        {
            return $"serviceOperation-{serviceName}-{operation}"; 
        }
        async Task IServcieOperationStorage.ServcieOperationStorage(IEnumerable<SpanServiceOperation> spanServiceOperation, CancellationToken cancellationToken)
        {
            if (spanServiceOperation == null)
                return;
            List<ServiceOperationModel> serviceOperationModels = new List<ServiceOperationModel>();
            foreach (var serviceOperation in spanServiceOperation.Where(w => !string.IsNullOrEmpty(w.Operation) &&
            w.Process != null
            && !string.IsNullOrEmpty(w.Process.ServiceName)))
            {
                var cacheKey = CreateCacheKey(serviceOperation.Process.ServiceName,serviceOperation.Operation);
                if (MemoryCache.TryGetValue(cacheKey, out var _))
                {
                    return;
                }
                MemoryCache.Set(cacheKey, true);
                serviceOperationModels.Add(new ServiceOperationModel()
                {
                    Operation = serviceOperation.Operation,
                    Service = serviceOperation.Process.ServiceName,
                });
            }
            if (serviceOperationModels.Count > 0)
            {
                var bulkRequest = new BulkRequest { Operations = new List<IBulkOperation>() };               
                foreach (var serviceOperationModel in serviceOperationModels)
                {
                    var operation = new BulkIndexOperation<ServiceOperationModel>(serviceOperationModel) { Index =_IndexName  };
                    bulkRequest.Operations.Add(operation);
                }
                var result = await ElasticClient.BulkAsync(bulkRequest, cancellationToken);
                foreach (var item in result.ItemsWithErrors)
                {
                    ServiceOperationModel source;
                    if ((source= item.GetResponse<ServiceOperationModel>()?.Source) != null)
                    {
                        var cacheKey = CreateCacheKey(source.Service,source.Operation);
                        MemoryCache.Remove(cacheKey);
                    }
                }
            }
        }
    }
}
