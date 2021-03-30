using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Model.Dto
{
    public class TracingQueryParameter : TimeRangeQueryParameter
    {
        public string ServiceName { get; set; }

        public string OperationName { get; set; }

        public Dictionary<string, object> Tags { get; set; }

        public int? DurationMin { get; set; }

        public int? DurationMax { get; set; }

        public int Limit { get; set; } = 10;

        public bool IsEmpty()
        {
            return !(StartTimeMin.HasValue && StartTimeMax.HasValue && !string.IsNullOrEmpty(ServiceName));
        }
    }
}
