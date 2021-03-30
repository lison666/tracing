using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lwaay.Tracing.Model.Dto
{
    public class TracingQueryDto
    {
        public IEnumerable<TracingDto> Data { get; set; }
        public int Total => Data == null ? 0 : Data.Count();
        public int Limit { get; set; }
        public int Offset { get; set; }
        public string Errors { get; set; } = null;
    }
}
