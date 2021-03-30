using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Model
{
    public class TracingBatch
    {
        public IEnumerable<Span> Spans { get; set; }

        public SpanProcess Process { get; set; }
    }
}
