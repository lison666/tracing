using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Model
{
    public class Span
    {
        public string TraceId { get; set; }

        public string SpanId { get; set; }

        public string OperationName { get; set; }

        public List<SpanReference> References { get; set; }

        public uint Flags { get; set; }

        public long StartTime { get; set; }

        public int Duration { get; set; }

        public List<SpanTag> Tags { get; set; }

        public List<SpanLog> Logs { get; set; }

        public SpanProcess Process { get; set; }

        public List<string> Warnings { get; set; }

    }
}
