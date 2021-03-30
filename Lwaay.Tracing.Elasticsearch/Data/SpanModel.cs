using Lwaay.Tracing.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Lwaay.Tracing.Elasticsearch.Data
{
    public class SpanModel
    {
        public string TraceId { get; set; }

        public string ParentSpanID { get; set; }

        public string SpanId { get; set; }

        public string ServiceName { get; set; }

        public string OperationName { get; set; }

        public List<SpanReference> References { get; set; }

        public uint Flags { get; set; }

        public long StartTime { get; set; }

        public int Duration { get; set; }

        public List<SpanTagModel> Tags { get; set; }

        public List<SpanLogModel> Logs { get; set; }

        public SpanProcessModel Process { get; set; }
    }

    public class SpanTagModel
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class SpanLogModel
    {
         public long Timestamp { get; set; }

        public SpanTagModel[] Fields { get; set; }
    }

    public class SpanProcessModelEqualityComparer : IEqualityComparer<SpanProcessModel>
    {
        public bool Equals([AllowNull] SpanProcessModel x, [AllowNull] SpanProcessModel y)
        {
            if (x == null || y == null)
                return false;
            return string.Equals(x.ServiceName, y.ServiceName) &&
                 x.Tags != null && y.Tags != null &&
                 x.Tags.All(c => y.Tags.Any(a => a.Key == c.Key && a.Value == c.Value));
        }

        public int GetHashCode([DisallowNull] SpanProcessModel obj)
        {
           
            int hashCode=obj.ServiceName.GetHashCode();
            if (obj.Tags != null)
            {
                foreach (var tag in obj.Tags)
                {
                    hashCode = hashCode ^ (tag.Key.GetHashCode()^tag.Value.GetHashCode());
                }
            }
            return hashCode;
        }
    }

    public class SpanProcessModel 
    {
        public string ServiceName { get; set; }

        public SpanTagModel[] Tags { get; set; }

        
    }
}
