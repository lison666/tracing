using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Model
{
    public class SpanServiceOperation
    {
        public SpanProcess Process { get; set; }

        public string Operation { get; set; }
    }
}
