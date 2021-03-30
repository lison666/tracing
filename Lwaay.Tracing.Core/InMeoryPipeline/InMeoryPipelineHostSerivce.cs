using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Core.InMeoryPipeline
{
    class InMeoryPipelineHostSerivce : IHostedService
    {
        IEnumerable<ITracingPipelineHander> _TracingPipelineHanders;
        ILogger<InMeoryPipelineHostSerivce> _Logger;
        public InMeoryPipelineHostSerivce(IEnumerable<ITracingPipelineHander> tracingPipelineHanders,
            ILogger<InMeoryPipelineHostSerivce> logger)
        {
            _TracingPipelineHanders = tracingPipelineHanders;
            _Logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _Logger.LogInformation("InMeoryPipelineHostSerivce Start");
            foreach (var item in _TracingPipelineHanders)
            {
                _Logger.LogInformation("{0} Start ",item.GetType().FullName);
                item.Start(cancellationToken);
            }
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _Logger.LogInformation("InMeoryPipelineHostSerivce Stop");
            foreach (var item in _TracingPipelineHanders)
            {
                await item.Stop(cancellationToken);
            }
        }
    }
}
