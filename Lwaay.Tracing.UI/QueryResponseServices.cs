using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lwaay.Tracing.UI
{
    public class QueryResponseServices<T>
    {
        public IEnumerable<T> Data { get; set; } = Array.Empty<T>();
        public int Total => Data.Count();
        public int Limit { get; set; }
        public int Offset { get; set; }
        public string Errors { get; set; } = null;
    }
}
