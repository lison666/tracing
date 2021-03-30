using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Core
{
    public interface ITracingPipelineHander
    {
        IDisposable Start(CancellationToken cancellationToken);

        Task Stop(CancellationToken cancellationToken);
    }
}
