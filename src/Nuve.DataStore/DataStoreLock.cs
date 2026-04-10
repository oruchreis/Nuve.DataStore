using System;
using System.Collections.Generic;
using System.Text;

namespace Nuve.DataStore;

/// <summary>
/// Represents a distributed lock that can be acquired and released.
/// </summary>
public abstract class DataStoreLock : IDisposable
#if !NET48
    , IAsyncDisposable
#endif
{
    /// <summary>
    /// Gets the date and time when the lock was successfully acquired, or null if the lock has not been acquired.
    /// </summary>
    public virtual DateTimeOffset? LockAchieved { get; protected set; }

    /// <inheriteddoc/>
    public abstract void Dispose();
#if !NET48
    /// <inheriteddoc/>
    public abstract ValueTask DisposeAsync();
#endif

    /// <summary>
    /// Attempts to extend the expiration time of the lock.
    /// </summary>
    /// <remarks>This method does not guarantee that the expiration will be extended if the lock has already
    /// expired or is no longer valid. The actual behavior may depend on the underlying data store
    /// implementation.</remarks>
    /// <param name="expire">The new expiration interval for the lock. If null, the default expiration is used.</param>
    /// <returns> The result is <see langword="true"/> if the expiration
    /// was successfully extended; otherwise, <see langword="false"/>.</returns>
    public abstract bool Extend(TimeSpan? expire = null);
    /// <summary>
    /// Attempts to extend the expiration time of the lock asynchronously.
    /// </summary>
    /// <remarks>This method does not guarantee that the expiration will be extended if the lock has already
    /// expired or is no longer valid. The actual behavior may depend on the underlying data store
    /// implementation.</remarks>
    /// <param name="expire">The new expiration interval for the lock. If null, the default expiration is used.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the expiration
    /// was successfully extended; otherwise, <see langword="false"/>.</returns>
    public abstract Task<bool> ExtendAsync(TimeSpan? expire = null);
    /// <summary>
    /// Releases the lock held by the current instance.
    /// </summary>
    /// <remarks>Call this method to release a previously acquired lock. The behavior when releasing a lock
    /// that is not currently held may vary depending on the implementation.</remarks>
    /// <returns><see langword="true"/> if the lock was successfully released; otherwise, <see langword="false"/>.</returns>
    public abstract bool Release();
    /// <summary>
    /// Asynchronously releases the lock held by the current instance.
    /// </summary>
    /// <remarks>Call this method to release a previously acquired lock. The result indicates whether the
    /// release was successful, which may be affected by the current lock state or ownership semantics. This method is
    /// intended to be used in asynchronous programming scenarios.</remarks>
    /// <returns>A task that represents the asynchronous release operation. The task result is <see langword="true"/> if the lock
    /// was successfully released; otherwise, <see langword="false"/>.</returns>
    public abstract Task<bool> ReleaseAsync();
}
