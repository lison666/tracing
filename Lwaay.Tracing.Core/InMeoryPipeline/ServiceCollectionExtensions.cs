using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Core.InMeoryPipeline
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInMeoryTracingPipeline(this IServiceCollection serviceDescriptors, Action<TracingTPLPipelineOption> action)
        {
            serviceDescriptors.AddOptions();
            serviceDescriptors.Configure<TracingTPLPipelineOption>(action);
            serviceDescriptors.AddSingleton<ITracingTPLPipeline, TracingTPLPipeline>();
            serviceDescriptors.AddSingleton<ITracingPipeline>(s=>s.GetService<ITracingTPLPipeline>());
            serviceDescriptors.AddSingleton<ITracingPipelineHander, TracingTPLPipelineHander>();
            serviceDescriptors.AddSingleton<IHostedService, InMeoryPipelineHostSerivce>();
            return serviceDescriptors;
        }
    }
}
