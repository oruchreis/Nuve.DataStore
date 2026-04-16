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
    /// Gets the token that uniquely identifies the owner of the lock.
    /// </summary>
    /// <remarks>
    /// This token is used internally by the lock provider to ensure that only the
    /// lock owner can renew or release the lock.
    /// 
    /// The value is typically generated when the lock is acquired and remains constant
    /// for the lifetime of the lock instance.
    /// 
    /// This token should be treated as an opaque value and must not be modified or reused
    /// across different lock instances.
    /// 
    /// <para>
    /// <b>Important:</b>
    /// This token is intended for lock ownership validation (e.g., during renew or release operations)
    /// and should not be used to prevent stale writes in distributed systems. For that purpose,
    /// use <see cref="FencingToken"/>.
    /// </para>
    /// </remarks>
    public virtual string? OwnerToken => null;

    /// <summary>
    /// Gets the fencing token associated with the current lock instance.
    /// </summary>
    /// <remarks>
    /// A fencing token is a monotonically increasing value that is assigned when a lock is acquired.
    /// It is used to prevent stale or out-of-order operations in distributed systems.
    /// 
    /// <para>
    /// Each time a lock is successfully acquired, a new fencing token is generated,
    /// and it is guaranteed to be greater than any previously issued token for the same lock key.
    /// </para>
    /// 
    /// <para>
    /// <b>Important usage pattern:</b>
    /// The fencing token must be validated by the system that performs the protected operation
    /// (e.g., a database, message processor, or external service).
    /// </para>
    /// 
    /// <para>
    /// Example:
    /// <code>
    /// UPDATE resource
    /// SET value = @value,
    ///     fencing_token = @incomingToken
    /// WHERE id = @id
    ///   AND fencing_token &lt; @incomingToken;
    /// </code>
    /// </para>
    /// 
    /// <para>
    /// If the update affects zero rows, it indicates that a newer lock owner has already
    /// performed an operation, and the current operation must be discarded.
    /// </para>
    /// 
    /// <para>
    /// <b>Why this is necessary:</b>
    /// In distributed environments, a lock may expire or be lost (e.g., due to network issues),
    /// and another process may acquire the same lock. The previous owner might still attempt
    /// to perform operations after losing the lock. The fencing token prevents such stale
    /// operations from being applied.
    /// </para>
    /// 
    /// <para>
    /// <b>Important:</b>
    /// The lock provider does not enforce fencing token validation. It is the responsibility
    /// of the consumer to validate this value in the target system.
    /// </para>
    /// </remarks>
    public virtual long FencingToken => 0;

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
    /// Asynchronously retrieves the remaining time to live (TTL) for the associated data store resource, if available.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="TimeSpan"/> indicating
    /// the remaining TTL, or <see langword="null"/> if the TTL is not set or cannot be determined.</returns>
    public abstract Task<TimeSpan?> GetTtlAsync();

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
