using Lwaay.Tracing.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lwaay.Tracing.Core.InMeoryPipeline
{
    interface ITracingTPLPipeline: ITracingPipeline
    {
        ISourceBlock<TracingBatch> SourceBlock { get; }
    }

    class TracingTPLPipeline : ITracingTPLPipeline
    {
        BroadcastBlock<TracingBatch> _BatchBlock = null;
        ILogger<TracingTPLPipeline> _Logger = null;
        public TracingTPLPipeline(ILogger<TracingTPLPipeline> logger)
        {
            _BatchBlock = new BroadcastBlock<TracingBatch>(x=>x);
            _Logger = logger;
        }

        public ISourceBlock<TracingBatch> SourceBlock => _BatchBlock;

        public async Task Post(TracingBatch tracingBatch, CancellationToken cancellationToken)
        {
            await _BatchBlock.SendAsync(tracingBatch, cancellationToken);
            _Logger.LogInformation("TracingTPLPipeline Post Process:{0}", tracingBatch.Process.ServiceName);
        }
    }
}
