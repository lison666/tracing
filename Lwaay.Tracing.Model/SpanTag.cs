using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Model
{
    public class SpanTag
    {
        public string Key { get; set; }

        /// <summary>
        /// <see cref="Jaeger.ApiV2.ValueType"/>
        /// </summary>
        public string Type { get; set; }
        public object Value { get; set; }

        public override int GetHashCode()
        {
            return Key.GetHashCode() ^ Type.GetHashCode() ^ Value.GetHashCode();
        }
    }
}
