using AutoMapper;
using Google.Protobuf.Collections;
using Grpc.Core;
using Jaeger.ApiV2;
using Lwaay.Tracing.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Grpc.Query
{
    public class GrpcQuery : Jaeger.ApiV2.QueryService.QueryServiceBase
    {
        IServiceQuery _ServiceQuery;
        ITracingQuery _TracingQuery;
        IMapper _Mapper;
        ILogger<GrpcQuery> _Logger;
        public GrpcQuery(IServiceQuery serviceQuery, ITracingQuery tracingQuery, IMapper mapper,
            ILogger<GrpcQuery> logger)
        {
            _ServiceQuery = serviceQuery;
            _TracingQuery = tracingQuery;
            _Mapper = mapper;
            _Logger = logger;
        }


        public override Task<ArchiveTraceResponse> ArchiveTrace(ArchiveTraceRequest request, ServerCallContext context)
        {
            return null;//base.ArchiveTrace(request, context);
        }

        public override async Task FindTraces(FindTracesRequest request, IServerStreamWriter<SpansResponseChunk> responseStream, ServerCallContext context)
        {
            var query = _Mapper.Map<Model.Dto.TracingQueryParameter>(request.Query);
            var result = await _TracingQuery.GetTracing(query, context.CancellationToken);
            SpansResponseChunk spansResponseChunk = new SpansResponseChunk();
            List<Jaeger.ApiV2.Span> spans = new List<Span>();
            foreach (var item in result)
            {
                var process = item.Processes;
                foreach (var spanModel in item.Spans)
                {
                    var span = _Mapper.Map<Span>(spanModel);
                    if (!string.IsNullOrEmpty(spanModel.ProcessId) &&
                        process.TryGetValue(spanModel.ProcessId, out var spanProcess) && spanProcess != null)
                    {
                        span.Process = _Mapper.Map<Jaeger.ApiV2.Process>(spanProcess);
                    }
                    spans.Add(span);
                }
            }
            var repeatedField_span = new RepeatedField<Span>();
            repeatedField_span.AddRange(spans);
            spansResponseChunk.Spans.Add(_Mapper.Map<RepeatedField<Span>>(repeatedField_span));
            await responseStream.WriteAsync(spansResponseChunk);
        }

        public override Task<GetDependenciesResponse> GetDependencies(GetDependenciesRequest request, ServerCallContext context)
        {
            //return base.GetDependencies(request, context);
            return null;
        }

        public override async Task<GetOperationsResponse> GetOperations(GetOperationsRequest request, ServerCallContext context)
        {
            var operationNames = await _ServiceQuery.GetOperation(request.Service, context.CancellationToken);
            GetOperationsResponse response = new GetOperationsResponse();
            response.OperationNames.Add(operationNames.Distinct().ToArray());
            return response;
        }

        public override async Task<GetServicesResponse> GetServices(GetServicesRequest request, ServerCallContext context)
        {
            var serviceName = await _ServiceQuery.GetServices(context.CancellationToken);
            GetServicesResponse getServicesResponse = new GetServicesResponse();
            getServicesResponse.Services.Add(serviceName.Distinct().ToArray());
            return getServicesResponse;
        }

        public override async Task GetTrace(GetTraceRequest request, IServerStreamWriter<SpansResponseChunk> responseStream, ServerCallContext context)
        {
            var traceId = _Mapper.Map<string>(request.TraceId);
            _Logger.Log(LogLevel.Information, "GrpcQuery.GetTrace.{0}", traceId);
            var result = await _TracingQuery.GetTracing(traceId, context.CancellationToken);
            SpansResponseChunk response = new SpansResponseChunk();
            var process = result.Processes;
            List<Jaeger.ApiV2.Span> spans = new List<Span>();
            foreach (var spanModel in result.Spans)
            {
                var span = _Mapper.Map<Span>(spanModel);
                if (!string.IsNullOrEmpty(spanModel.ProcessId) &&
                    process.TryGetValue(spanModel.ProcessId, out var spanProcess) && spanProcess != null)
                {
                    span.Process = _Mapper.Map<Jaeger.ApiV2.Process>(spanProcess);
                }
                spans.Add(span);
            }
            var repeatedField_span = new RepeatedField<Span>();
            repeatedField_span.AddRange(spans);
            response.Spans.Add(_Mapper.Map<RepeatedField<Span>>(repeatedField_span));
            await responseStream.WriteAsync(response);
        }
    }
}
