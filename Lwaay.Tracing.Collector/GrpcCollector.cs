using AutoMapper;
using Grpc.Core;
using Grpc.Core.Logging;
using Jaeger.ApiV2;
using Lwaay.Tracing.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Grpc.Collector
{
    public class GrpcCollector : CollectorService.CollectorServiceBase
    {
        ITracingPipeline _TracingPipeline;
        ILogger<GrpcCollector> _Logger;
        IMapper _Mapper;
        public GrpcCollector(ITracingPipeline tracingPipeline, ILogger<GrpcCollector> logger,
            IMapper mapper)
        {
            _TracingPipeline = tracingPipeline;
            _Logger = logger;
            _Mapper = mapper;
        }

        public override async Task<PostSpansResponse> PostSpans(PostSpansRequest request, ServerCallContext context)
        {
            _Logger.LogInformation("PostSpan ServiceName:{0}", request.Batch.Process.ServiceName);
            await _TracingPipeline.Post(new Model.TracingBatch()
            {
                Process = _Mapper.Map<Model.SpanProcess>(request.Batch.Process),
                Spans = _Mapper.Map<IEnumerable<Model.Span>>(request.Batch.Spans),
            }, default(CancellationToken));
            return new PostSpansResponse();
        }
    }
}
