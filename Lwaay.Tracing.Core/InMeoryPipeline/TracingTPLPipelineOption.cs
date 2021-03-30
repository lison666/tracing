using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Core.InMeoryPipeline
{
    public class TracingTPLPipelineOption
    {
        public int MaxHanderParallelism { get; set; }


        public int BatchSpanSize { get; set; }

        public int BatchProcessSize { get; set; }

    }
}
