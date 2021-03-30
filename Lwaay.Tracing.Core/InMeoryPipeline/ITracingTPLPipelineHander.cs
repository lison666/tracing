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
    interface ITracingTPLPipelineHander : ITracingPipelineHander
    {
    }

    class TracingTPLPipelineHander : ITracingTPLPipelineHander
    {
        IEnumerable<ITracingTPLPipeline> _TracingTPLPipelines = null;
        TracingTPLPipelineOption _TracingTPLPipelineOption;
        ITracingStorage _TracingStorage;
        IServcieOperationStorage _ServcieOperationStorage;
        IDisposable Disposable = null;
        ILogger<TracingTPLPipelineHander> _Logger = null;
        private readonly int DEFAUKT_CONSUMER_PARALLELISM = Environment.ProcessorCount;

        public TracingTPLPipelineHander(IEnumerable<ITracingTPLPipeline> tracingTPLPipeline,
            IOptions<TracingTPLPipelineOption> tracingTPLPipelineOption,
            ITracingStorage tracingStorage, IServcieOperationStorage servcieOperationStorage,
            ILogger<TracingTPLPipelineHander> logger)
        {
            _TracingTPLPipelines = tracingTPLPipeline;
            _TracingTPLPipelineOption = tracingTPLPipelineOption.Value;
            _TracingStorage = tracingStorage;
            _ServcieOperationStorage = servcieOperationStorage;
            _Logger = logger;
        }
        private class CollectionDisposable : IDisposable
        {
            IEnumerable<IDisposable> Disposables;
            public CollectionDisposable(IEnumerable<IDisposable> disposables)
            {
                Disposables = disposables;
            }
            public void Dispose()
            {
                foreach (var item in Disposables)
                {
                    item.Dispose();
                }
            }
        }
        private void CreateSpanServiceOperation(IEnumerable<SpanServiceOperation> spanProcesses)
        {
            _ServcieOperationStorage.ServcieOperationStorage(spanProcesses, default(CancellationToken))
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void CreateSpan(IEnumerable<Span> spans)
        {
            _TracingStorage.TracingStorage(spans, default(CancellationToken))
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public IDisposable Start(CancellationToken cancellationToken)
        {
            _Logger.LogInformation("TracingTPLPipelineHander Start");
            List<IDisposable> disposables = new List<IDisposable>();
            var batchSpan = new BatchBlock<Span>(_TracingTPLPipelineOption.BatchSpanSize);
            var batchProcess = new BatchBlock<SpanServiceOperation>(_TracingTPLPipelineOption.BatchProcessSize);

            disposables.Add(batchSpan.LinkTo(new ActionBlock<IEnumerable<Span>>(CreateSpan, new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = _TracingTPLPipelineOption.MaxHanderParallelism<=0? DEFAUKT_CONSUMER_PARALLELISM: _TracingTPLPipelineOption.MaxHanderParallelism,
            })));
            disposables.Add(batchProcess.LinkTo(new ActionBlock<IEnumerable<SpanServiceOperation>>(CreateSpanServiceOperation)));
            foreach (var item in _TracingTPLPipelines)
            {
                var disposable = item.SourceBlock.LinkTo(new ActionBlock<TracingBatch>(c =>
                {
                    foreach (var item in c.Spans)
                    {
                        item.Process = c.Process;
                        batchSpan.Post(item);
                        batchProcess.Post(new SpanServiceOperation()
                        {
                            Operation = item.OperationName,
                            Process = c.Process
                        });
                    }
                }), t => t != null && t.Spans != null);
                disposables.Add(disposable);
            }
            Disposable = new CollectionDisposable(disposables);
            return Disposable;
        }

        public Task Stop(CancellationToken cancellationToken)
        {
            foreach (var item in _TracingTPLPipelines)
            {
                item.SourceBlock.Complete();
            }
            Disposable?.Dispose();
            return Task.CompletedTask;
        }
    }
}
