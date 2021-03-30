using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Model.Dto
{
    public class TracingDto
    {
        public string TraceID { get; set; }

        public IEnumerable<SpanDto> Spans { get; set; }

        public Dictionary<string, SpanProcess> Processes { get; set; }

        public string[] Warnings { get; set; }
    }
}
