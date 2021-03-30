using AutoMapper;
using Lwaay.Tracing.Core;
using Lwaay.Tracing.Elasticsearch.Data;
using Lwaay.Tracing.Model;
using Lwaay.Tracing.Model.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Elasticsearch
{
    class SpanService : ITracingStorage, ITracingQuery
    {
        IMemoryCache MemoryCache;
        ElasticClient ElasticClient;
        ILogger<SpanService> Logger;
        IOptions<ElasticsearchOption> _TracingTPLPipelineOption;
        IMapper _Mapper;
        private static object _lock = new object();
        private const int _MaxTracingSpanCount = 1000;
        public SpanService(IMemoryCache memoryCache,
            IElasticClientFactory elasticClientFactory,
            ILogger<SpanService> logger,
            IOptions<ElasticsearchOption> tracingTPLPipelineOption,
            IMapper mapper)
        {
            MemoryCache = memoryCache;
            ElasticClient = elasticClientFactory.Get();
            Logger = logger;
            _TracingTPLPipelineOption = tracingTPLPipelineOption;
            _Mapper = mapper;
        }
        private IndexName QueryIndexName = "tracing_span";
        private IndexName GetIndexName()
        {
            var indexName = $"tracing_span_{ DateTime.UtcNow.Date.ToString("yyyyMMdd")}";

            if (MemoryCache.TryGetValue(indexName, out IndexName index))
                return index;
            else
            {
                index = (IndexName)indexName;
                TryCreateIndex(index);
                MemoryCache.Set(indexName, index, new MemoryCacheEntryOptions()
                {
                    Size = 1,
                    SlidingExpiration = TimeSpan.FromDays(1),
                });
                return index;
            }
        }
        private void TryCreateIndex(IndexName indexName)
        {
            lock (_lock)
            {
                if (!ElasticClient.Indices.Exists(indexName).Exists)
                {
                    var tracingIndex = new CreateIndexDescriptor(indexName);

                    tracingIndex.Map<SpanModel>(m => m
                            .AutoMap()
                            .Properties(p => p.Keyword(t => t.Name(n => n.TraceId)))
                            .Properties(p => p.Keyword(t => t.Name(n => n.SpanId)))
                            .Properties(p => p.Nested<SpanTag>(n => n.Name(name => name.Tags).AutoMap()
                            .Properties(tag => tag.Keyword(k => k.Name(n => n.Key)))
                            .Properties(tag => tag.Keyword(k => k.Name(n => n.Value)))
                            .Properties(tag => tag.Keyword(k => k.Name(n => n.Type)))))
                            .Properties(p => p.Nested<SpanLog>(n => n.Name(name => name.Logs).AutoMap()
                            .Properties(p => p.Nested<SpanTag>(n => n.Name(name => name.Fields).AutoMap()
                                 .Properties(tag => tag.Keyword(k => k.Name(n => n.Value)))
                              .Properties(tag => tag.Keyword(k => k.Name(n => n.Type)))))))
                            .Properties(p => p.Nested<SpanProcess>(n => n.Name(name => name.Process).AutoMap().
                            Properties(p => p.Nested<SpanTag>(n => n.Name(name => name.Tags)
                              .AutoMap().Properties(tag => tag.Keyword(k => k.Name(n => n.Key)))
                              .Properties(tag => tag.Keyword(k => k.Name(n => n.Value)))
                              .Properties(tag => tag.Keyword(k => k.Name(n => n.Type)))
                            ))))
                            .Properties(p => p.Nested<SpanReference>(n => n.Name(name => name.References).AutoMap())));
                    var result = ElasticClient.Indices.Create(indexName, c => tracingIndex);
                    ElasticClient.Indices.BulkAlias(c => c.Add(a => a.Alias(QueryIndexName.Name).Index(indexName.Name)));
                }
            }

        }



        public async Task TracingStorage(IEnumerable<Span> spans, CancellationToken cancellationToken)
        {
            if (spans == null || spans.Count() <= 0)
                return;
            //List<SpanModel> spanModels = new List<SpanModel>(spans.Count());
            var bulkRequest = new BulkRequest { Operations = new List<IBulkOperation>() };
            var indexName = GetIndexName();
            foreach (var span in spans)
            {
                var spanmodel = _Mapper.Map<SpanModel>(span);
                var operation = new BulkIndexOperation<SpanModel>(spanmodel) { Index = indexName };
                bulkRequest.Operations.Add(operation);
            }
            var result = await ElasticClient.BulkAsync(bulkRequest, cancellationToken);
        }

        public async Task<IEnumerable<TracingDto>> GetTracing(TracingQueryParameter tracingQueryParameter, CancellationToken cancellationToken)
        {
            if (tracingQueryParameter.IsEmpty())
                return Enumerable.Empty<TracingDto>();
            var buildQuerys = BuildMustQuery(tracingQueryParameter).ToList();
            var query = BuildQuery(buildQuerys);


            var traceid_aggregation = await ElasticClient.SearchAsync<SpanModel>(s => s.Index(QueryIndexName).Size(0).Query(query)
             .Aggregations(a => a.Terms("group_traceId", t =>
                 t.Aggregations(sub => sub.Min("min_timestapm", m => m.Field(f => f.StartTime)))
                 .Field(f => f.TraceId).Order(o => o.Descending("min_timestapm")).Size(tracingQueryParameter.Limit)
             )));


            var traceIdsAggregations = traceid_aggregation.Aggregations.FirstOrDefault().Value as BucketAggregate;

            if (traceIdsAggregations == null || traceIdsAggregations.Items.Count <= 0)
                return Enumerable.Empty<TracingDto>();

            var keyBuckets = traceIdsAggregations.Items.Cast<KeyedBucket<object>>();

            var limit = -1;

            var tracIds = keyBuckets.Where(w => w.Key != null).Select(s => s.Key.ToString()).ToList();

            var result = await ElasticClient.SearchAsync<SpanModel>(s => s.Index(QueryIndexName)
               .Query(q => q.Terms(t => t.Field(f => f.TraceId).Terms(tracIds))).Take(limit));
            List<TracingDto> tracingDtos = new List<TracingDto>();
            int i = 0;
            var processModel = result.Documents.Select(s => s.Process).Distinct(new SpanProcessModelEqualityComparer())
    .ToDictionary(d => $"p{i++}", d => d);
            foreach (var item in result.Documents.GroupBy(g => g.TraceId))
            {
                var tracing = Convert(item.ToArray(), processModel);
                tracingDtos.Add(tracing);
            }
            return tracingDtos;
        }

        private TracingDto Convert(IEnumerable<SpanModel> spanModel
            , Dictionary<string, SpanProcessModel> processModel)
        {
            Dictionary<string, SpanProcess> dict_process = new Dictionary<string, SpanProcess>();
            SpanProcessModelEqualityComparer spanProcessModelEquality = new SpanProcessModelEqualityComparer();
            var tracing = new TracingDto()
            {
                TraceID = spanModel.FirstOrDefault()?.TraceId,
                Warnings = new string[0],
                Spans = spanModel.Select(s => _Mapper.Map<SpanDto>(s, m =>
                {
                    m.AfterMap((v, dto) =>
                    {
                        SpanModel model = null;
                        if ((model = v as SpanModel) != null)
                        {
                            var process = processModel.FirstOrDefault(f => spanProcessModelEquality.Equals(f.Value, model.Process));
                            dto.ProcessId = process.Key ?? "--";
                            if (process.Value != null && !dict_process.ContainsKey(dto.ProcessId))
                            {
                                dict_process.Add(dto.ProcessId, _Mapper.Map<SpanProcess>(process.Value));
                                ;
                            }
                        }
                    });
                })).OrderBy(a => a.StartTime).ToArray()
            };
            tracing.Processes = dict_process;
            return tracing;
        }

        private Func<QueryContainerDescriptor<SpanModel>, QueryContainer> BuildQuery(IEnumerable<Func<QueryContainerDescriptor<SpanModel>, QueryContainer>> q)
        {
            return query => query.Bool(b => b.Must(q));
        }

        private IEnumerable<Func<QueryContainerDescriptor<SpanModel>, QueryContainer>> BuildMustQuery(TracingQueryParameter tracingQueryParameter)
        {
            if (!string.IsNullOrEmpty(tracingQueryParameter.ServiceName))
            {
                yield return q => q.Term(new Field("serviceName.keyword"), tracingQueryParameter.ServiceName);
            }
            if (!string.IsNullOrEmpty(tracingQueryParameter.OperationName))
            {
                yield return q => q.Term(new Field("operationName.keyword"), tracingQueryParameter.OperationName);
            }
            if (tracingQueryParameter.StartTimeMin.HasValue)
            {
                yield return q => q.LongRange(d => d.Field(x => x.StartTime).GreaterThanOrEquals(tracingQueryParameter.StartTimeMin));
            }
            if (tracingQueryParameter.StartTimeMax.HasValue)
            {
                yield return q => q.LongRange(d => d.Field(x => x.StartTime).LessThan(tracingQueryParameter.StartTimeMax));
            }
            if (tracingQueryParameter.DurationMin.HasValue)
            {
                yield return q => q.Range(d => d.Field(x => x.Duration).GreaterThanOrEquals(tracingQueryParameter.DurationMin.Value));
            }
            if (tracingQueryParameter.DurationMax.HasValue)
            {
                yield return q => q.Range(d => d.Field(x => x.Duration).LessThan(tracingQueryParameter.DurationMax.Value));
            }
            if (tracingQueryParameter.Tags != null && tracingQueryParameter.Tags.Count > 0)
            {
                foreach (var tag in tracingQueryParameter.Tags)
                {
                    yield return q => q.Nested(n => n.Path(x => x.Tags).Query(q1 => q1.Bool(b => b.Must(f => f.Term(new Field("tags.key"), tag.Key), f => f.Term(new Field("tags.value"),
                        tag.Value?.ToString())))));
                }
            }
        }

        public async Task<TracingDto> GetTracing(string tracingId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tracingId))
                return null;
            var result = await ElasticClient.SearchAsync<SpanModel>(s => s.Index(QueryIndexName)
             .Query(q => q.Term(new Field("traceId"), tracingId))
             .Size(_MaxTracingSpanCount)
             );
            int i = 0;
            var processModel = result.Documents.Select(s => s.Process).Distinct()
                .ToDictionary(d => $"p{i++}", d => d);
            return Convert(result.Documents, processModel);
        }
    }
}
