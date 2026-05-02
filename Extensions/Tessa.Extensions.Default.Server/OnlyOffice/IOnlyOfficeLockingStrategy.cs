#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    public interface IOnlyOfficeLockingStrategy
    {
        Task<IAsyncDisposable> AcquireLockAsync(Guid id, bool readerLock, CancellationToken cancellationToken = default);
    }

}
