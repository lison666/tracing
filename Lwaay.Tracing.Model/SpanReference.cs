using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Model
{
    public class SpanReference
    {
        public string TraceId { get; set; }
        public string SpanId { get; set; }

        /// <summary>
        /// <para><see cref="Jaeger.ApiV2.SpanRefType"/></para>
        /// </summary>
        public SpanRefType RefType { get; set; }
    }
}
