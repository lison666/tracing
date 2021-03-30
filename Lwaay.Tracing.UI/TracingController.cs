using AutoMapper;
using Google.Protobuf;
using Grpc.Core;
using Lwaay.Tracing.Model;
using Lwaay.Tracing.Model.Dto;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lwaay.Tracing.UI
{
    [ApiController]
    [Route("/api")]
    public class TracingController : Controller
    {
        Jaeger.ApiV2.QueryService.QueryServiceClient _QueryServiceClient;
        IMapper _Mapper;
        public TracingController(Jaeger.ApiV2.QueryService.QueryServiceClient queryServiceClient,
            IMapper mapper)
        {
            _QueryServiceClient = queryServiceClient;
            _Mapper = mapper;
        }

        /// <summary>
        /// 查询所有的服务
        /// </summary>
        /// <returns></returns>
        [HttpGet("services")]
        public async Task<QueryResponseServices<string>> Services()
        {
            var result = await _QueryServiceClient.GetServicesAsync(new Jaeger.ApiV2.GetServicesRequest(),
                new CallOptions());
            return new QueryResponseServices<string>()
            {
                Data = _Mapper.Map<List<string>>(result.Services),
            };
        }

        /// <summary>
        /// 查询服务
        /// </summary>
        /// <returns></returns>
        [HttpGet("/traces/{traceId}")]
        public async Task<QueryResponseServices<TracingDto>> Services(string traceId)
        {
            if (string.IsNullOrEmpty(traceId))
                return new QueryResponseServices<TracingDto>()
                {
                    Data = new TracingDto[0],
                };
            var result = _QueryServiceClient.GetTrace(new Jaeger.ApiV2.GetTraceRequest()
            {
                TraceId = ByteString.CopyFrom(Jaeger.TraceId.FromString(traceId).ToByteArray())
            });
            List<SpanDto> spans = new List<SpanDto>();
            Dictionary<string, SpanProcess> dict_process = new Dictionary<string, SpanProcess>();
            while (await result.ResponseStream.MoveNext(CancellationToken.None))
            {
                foreach (var span in result.ResponseStream.Current.Spans)
                {
                    if (!dict_process.ContainsKey(span.ProcessId))
                        dict_process.Add(span.ProcessId, _Mapper.Map<SpanProcess>(span.Process));
                    spans.Add(_Mapper.Map<SpanDto>(span));
                }

            }
            List<TracingDto> tracingDtos = new List<TracingDto>();
            tracingDtos.Add(Convert(spans, dict_process));
            return new QueryResponseServices<TracingDto>()
            {
                Data = tracingDtos
            };
        }

        private TracingDto Convert(IEnumerable<SpanDto> spanModel, Dictionary<string, SpanProcess> processes)
        {
            return new TracingDto()
            {
                Processes = processes.Where(w => spanModel.Select(s => s.ProcessId).Contains(w.Key)).ToDictionary(d => d.Key, d => d.Value),
                Spans = spanModel,
                TraceID = spanModel.FirstOrDefault()?.TraceId,
                Warnings = new string[0]
            };
        }

        /// <summary>
        /// 查询一个 service 中的 Operation 
        /// </summary>
        /// <returns></returns>
        [HttpGet("services/{service}/operations")]
        public async Task<QueryResponseServices<string>> ServiceOperation(string service)
        {
            if (string.IsNullOrWhiteSpace(service))
                return new QueryResponseServices<string>();
            var result = await _QueryServiceClient.GetOperationsAsync(new Jaeger.ApiV2.GetOperationsRequest()
            {
                Service = service,
            });
            return new QueryResponseServices<string>()
            {
                Data = _Mapper.Map<List<string>>(result.OperationNames),
            };
        }

        /// <summary>
        /// 聚合搜索
        /// </summary>
        /// <returns></returns>
        [HttpGet("traces")]
        public async Task<QueryResponseServices<TracingDto>> Traces(
            [Required] string service,
            string operation,
            string tags,
            int? minDuration,
            int? maxDuration,
            [Required] long start,
            [Required] long end,
            string lookback,
            int limit = 10
            )
        {
            if (string.IsNullOrWhiteSpace(service))
                return new QueryResponseServices<TracingDto>();
            var request = new Jaeger.ApiV2.FindTracesRequest()
            {
                Query = new Jaeger.ApiV2.TraceQueryParameters()
                {
                    ServiceName = service,
                }
            };
            if (!string.IsNullOrEmpty(operation))
                request.Query.OperationName = operation;
            if (!string.IsNullOrEmpty(tags))
            {
                try
                {
                    var request_tags = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tags);
                    request.Query.Tags.Add(request_tags);
                }
                catch (Exception ex)
                {
                    //logger.LogError(ex, "Tags 格式有误！", tags);
                    return new QueryResponseServices<TracingDto>()
                    {
                        Errors = "Tags 格式有误！"
                    };
                }
            }
            if (minDuration.HasValue)
                request.Query.DurationMin = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(TimeSpan.FromSeconds(minDuration.Value));
            if (maxDuration.HasValue)
                request.Query.DurationMax = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(TimeSpan.FromSeconds(maxDuration.Value));
            request.Query.StartTimeMin = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(DateTimeOffset.FromUnixTimeMilliseconds(start));
            request.Query.StartTimeMax = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(DateTimeOffset.FromUnixTimeMilliseconds(end));
            var result = _QueryServiceClient.FindTraces(request);
            List<SpanDto> spans = new List<SpanDto>();
            Dictionary<string, SpanProcess> dict_process = new Dictionary<string, SpanProcess>();
            while (await result.ResponseStream.MoveNext(CancellationToken.None))
            {
                foreach (var span in result.ResponseStream.Current.Spans)
                {
                    if (!dict_process.ContainsKey(span.ProcessId))
                        dict_process.Add(span.ProcessId, _Mapper.Map<SpanProcess>(span.Process));
                    spans.Add(_Mapper.Map<SpanDto>(span));
                }

            }
            List<TracingDto> tracingDtos = new List<TracingDto>();
            foreach (var item in spans.GroupBy(g => g.TraceId))
            {
                tracingDtos.Add(Convert(item, dict_process));
            }
            return new QueryResponseServices<TracingDto>()
            {
                Data = tracingDtos
            };
        }
    }
}
