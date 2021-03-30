using Lwaay.Tracing.Model.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Core
{
    public interface ITracingQuery
    {
        Task<IEnumerable<TracingDto>> GetTracing(TracingQueryParameter tracingQueryParameter, CancellationToken cancellationToken);

        Task<TracingDto> GetTracing(string tracingId, CancellationToken cancellationToken);
    }
}
