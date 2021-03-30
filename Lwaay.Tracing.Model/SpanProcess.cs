using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;

namespace Lwaay.Tracing.Model
{
    public class SpanProcessEqualityComparer : IEqualityComparer<SpanProcess>
    {
        public bool Equals([AllowNull] SpanProcess x, [AllowNull] SpanProcess y)
        {
            if (x == null || y == null)
                return false;
            return string.Equals(x.ServiceName, y.ServiceName) &&
                x.Tags != null && y.Tags != null &&
                x.Tags.All(c => y.Tags.Any(a => a.Key == c.Key && a.Value == c.Value));
        }

        public int GetHashCode([DisallowNull] SpanProcess obj)
        {
            int hashCode = obj.ServiceName.GetHashCode();
            if (obj.Tags != null)
            {
                foreach (var tag in obj.Tags)
                {
                    hashCode = hashCode ^ (tag.Key.GetHashCode() ^ tag.Value.GetHashCode());
                }
            }
            return hashCode;
        }
    }

    public class SpanProcess 
    {
        public string ServiceName { get; set; }

        public SpanTag[] Tags { get; set; }

       
    }
}
