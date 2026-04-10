using System;
using System.Collections.Generic;
using System.Text;

namespace Nuve.DataStore;

public abstract class Lock : IDisposable
#if !NET48
    , IAsyncDisposable
#endif
{
    public virtual DateTimeOffset? LockAchieved { get; protected set; }

    public abstract void Dispose();
#if !NET48
    public abstract ValueTask DisposeAsync();
#endif

    public abstract bool Extend(TimeSpan? expire = null);
    public abstract Task<bool> ExtendAsync(TimeSpan? expire = null);
    public abstract bool Release();
    public abstract Task<bool> ReleaseAsync();
}
