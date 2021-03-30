using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Model
{
    public class SpanLog
    {
        public long Timestamp { get; set; }

        public SpanTag[] Fields { get; set; }
    }
}
