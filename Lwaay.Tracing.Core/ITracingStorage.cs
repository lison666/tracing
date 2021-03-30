using Lwaay.Tracing.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Core
{
    public interface ITracingStorage
    {

        Task TracingStorage(IEnumerable<Span> spans, CancellationToken cancellationToken);
    }
}
