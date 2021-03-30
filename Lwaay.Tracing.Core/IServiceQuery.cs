using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Core
{
    public interface IServiceQuery
    {
        Task<IEnumerable<string>> GetServices(CancellationToken cancellationToken);

        Task<IEnumerable<string>> GetOperation(string serviceName, CancellationToken cancellationToken);

    }
}
