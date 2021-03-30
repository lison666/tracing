using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Lwaay.Tracing.Model.Dto
{
    public class SpanDto
    {
        [JsonPropertyName("traceID")]
        public string TraceId { get; set; }
        [JsonPropertyName("spanID")]
        public string SpanId { get; set; }

        public string OperationName { get; set; }

        public List<SpanReference> References { get; set; }

        public uint Flags { get; set; }

        public long StartTime { get; set; }

        public int Duration { get; set; }

        public List<SpanTag> Tags { get; set; }

        public List<SpanLog> Logs { get; set; }
        [JsonPropertyName("processID")]
        public string ProcessId { get; set; }

        public List<string> Warnings { get; set; }

    }
}
