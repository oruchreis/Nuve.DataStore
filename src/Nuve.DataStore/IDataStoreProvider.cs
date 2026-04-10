using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    /// <summary>
    /// Defines the contract for a data store provider, including initialization, key management,
    /// expiration handling, and distributed locking capabilities.
    /// </summary>
    public interface IDataStoreProvider
    {
        /// <summary>
        /// Initializes the data store provider with the specified connection string and optional profiler.
        /// </summary>
        /// <param name="connectionString">The connection string used to establish connections to the data store. Cannot be null or empty.</param>
        /// <param name="profiler">An optional profiler instance used to monitor data store operations, or null if profiling is not required.</param>
        void Initialize(string connectionString, IDataStoreProfiler? profiler);
        /// <summary>
        /// Asynchronously initializes the data store provider with the specified connection string and optional profiler.
        /// </summary>
        /// <param name="connectionString">The connection string used to establish connections to the data store. Cannot be null or empty.</param>
        /// <param name="profiler">An optional profiler instance used to monitor data store operations, or null if profiling is not required.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        Task InitializeAsync(string connectionString, IDataStoreProfiler? profiler);
        /// <summary>
        /// Gets the type of the specified key in the data store.
        /// </summary>
        /// <param name="key">The key whose type is to be determined. Cannot be null or empty.</param>
        /// <returns>A value of the StoreKeyType enumeration that indicates the type of the specified key.</returns>
        StoreKeyType GetKeyType(string key);
        /// <summary>
        /// Asynchronously retrieves the type of the specified key from the data store.
        /// </summary>
        /// <param name="key">The key whose type is to be determined. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the type of the specified key.</returns>
        Task<StoreKeyType> GetKeyTypeAsync(string key);
        /// <summary>
        /// Gets the expiration time associated with the specified key, if available.
        /// </summary>
        /// <param name="key">The key whose expiration time is to be retrieved. Cannot be null.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the time until the key expires, or <see langword="null"/> if the key
        /// does not have an expiration set.</returns>
        TimeSpan? GetExpire(string key);
        /// <summary>
        /// Gets the remaining time to live (TTL) for the specified key, if it exists.
        /// </summary>
        /// <param name="key">The key whose expiration time is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the time remaining until the key expires, or <see langword="null"/> if
        /// the key does not exist or does not have an expiration set.</returns>
        Task<TimeSpan?> GetExpireAsync(string key);
        /// <summary>
        /// Sets the expiration time for the specified key.
        /// </summary>
        /// <param name="key">The key whose expiration time is to be set. Cannot be null or empty.</param>
        /// <param name="expire">The time interval after which the key will expire. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
        /// <returns>true if the expiration was successfully set; otherwise, false.</returns>
        bool SetExpire(string key, TimeSpan expire);
        /// <summary>
        /// Asynchronously sets the expiration time for the specified key.
        /// </summary>
        /// <param name="key">The key whose expiration time is to be set. Cannot be null or empty.</param>
        /// <param name="expire">The duration after which the key will expire and be removed. Must be a positive time span.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the
        /// expiration was set successfully; otherwise, <see langword="false"/>.</returns>
        Task<bool> SetExpireAsync(string key, TimeSpan expire);
        /// <summary>
        /// Removes the value associated with the specified key from the data store.
        /// </summary>
        /// <param name="key">The key of the element to remove. Cannot be null.</param>
        /// <returns>true if the element is successfully removed; otherwise, false. This method also returns false if the key was
        /// not found in the data store.</returns>
        bool Remove(string key);        
        /// <summary>
        /// Asynchronously removes the value associated with the specified key from the data store.
        /// </summary>
        /// <param name="key">The key of the item to remove. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the item was
        /// successfully removed; otherwise, <see langword="false"/>.</returns>
        Task<bool> RemoveAsync(string key);  
        /// <summary>
        /// Executes the specified action within a distributed lock, waiting up to the specified timeout to acquire the
        /// lock. Optionally skips or throws if the lock cannot be acquired within the timeout period.
        /// </summary>
        /// <remarks>If both skipWhenTimeout and throwWhenTimeout are false, the method will block until
        /// the lock is acquired. Use slidingExpire to ensure the lock does not expire while the action is running,
        /// especially for long-running operations.</remarks>
        /// <param name="lockKey">The unique key that identifies the lock to acquire. Cannot be null or empty.</param>
        /// <param name="waitTimeout">The maximum duration to wait for the lock to become available before timing out.</param>
        /// <param name="action">The action to execute while the lock is held. Cannot be null.</param>
        /// <param name="slidingExpire">The duration to extend the lock's expiration after each successful action execution, helping to prevent
        /// premature lock release during long-running actions.</param>
        /// <param name="skipWhenTimeout">true to skip executing the action if the lock cannot be acquired within the timeout; otherwise, false to
        /// proceed based on the value of throwWhenTimeout. The default is true.</param>
        /// <param name="throwWhenTimeout">true to throw an exception if the lock cannot be acquired within the timeout; otherwise, false to silently
        /// skip the action. The default is false.</param>
        void Lock(string lockKey, TimeSpan waitTimeout, Action action, TimeSpan slidingExpire, bool skipWhenTimeout = true, bool throwWhenTimeout = false);
        /// <summary>
        /// Attempts to acquire a distributed lock identified by the specified key, with a sliding expiration and
        /// optional timeout behavior.
        /// </summary>
        /// <remarks>The lock will automatically expire after the specified sliding expiration unless it
        /// is renewed. If throwWhenTimeout is true and the lock cannot be acquired within the allowed time, an
        /// exception is thrown. Callers should dispose of the returned Lock object to release the lock when it is no
        /// longer needed.</remarks>
        /// <param name="lockKey">The unique key that identifies the lock to acquire. Cannot be null or empty.</param>
        /// <param name="waitCancelToken">A token to monitor for cancellation requests. The operation is canceled if the token is triggered.</param>
        /// <param name="slidingExpire">The duration for which the lock will be held before it expires, unless renewed.</param>
        /// <param name="throwWhenTimeout">true to throw an exception if the lock cannot be acquired within the timeout period; otherwise, false to
        /// return null on timeout.</param>
        /// <returns>A Lock object representing the acquired lock if successful; otherwise, null if the lock could not be
        /// acquired and throwWhenTimeout is false.</returns>
        DataStoreLock? AcquireLock(string lockKey, CancellationToken waitCancelToken, TimeSpan slidingExpire, bool throwWhenTimeout);
        /// <summary>
        /// Attempts to acquire a distributed lock identified by the specified key and, if successful, executes the
        /// provided asynchronous action within the lock's context.
        /// </summary>
        /// <remarks>If both skipWhenTimeout and throwWhenTimeout are false, the method will wait for the
        /// lock up to the specified timeout and do nothing if the lock is not acquired. If throwWhenTimeout is true, an
        /// exception will be thrown on timeout. If skipWhenTimeout is true, the action will be skipped without error on
        /// timeout. The slidingExpire parameter is used to extend the lock's lifetime during long-running actions to
        /// prevent lock expiration.</remarks>
        /// <param name="lockKey">The unique key that identifies the lock to acquire. Cannot be null or empty.</param>
        /// <param name="waitTimeout">The maximum duration to wait for the lock to become available before timing out.</param>
        /// <param name="action">The asynchronous action to execute if the lock is successfully acquired.</param>
        /// <param name="slidingExpire">The duration to extend the lock's expiration after each successful action execution, helping to prevent
        /// premature lock release during long-running operations.</param>
        /// <param name="skipWhenTimeout">true to silently skip execution of the action if the lock cannot be acquired within the wait timeout;
        /// otherwise, false.</param>
        /// <param name="throwWhenTimeout">true to throw an exception if the lock cannot be acquired within the wait timeout; otherwise, false.</param>
        /// <returns>A task that represents the asynchronous operation. The task completes when the action has finished executing
        /// or when the lock acquisition attempt has concluded.</returns>
        Task LockAsync(string lockKey, TimeSpan waitTimeout, Func<Task> action, TimeSpan slidingExpire, bool skipWhenTimeout = true, bool throwWhenTimeout = false);
        /// <summary>
        /// Attempts to acquire a distributed lock asynchronously using the specified key and expiration policy.
        /// </summary>
        /// <remarks>Use this method to coordinate access to shared resources across distributed systems.
        /// The sliding expiration ensures the lock remains active as long as it is periodically renewed. If
        /// throwWhenTimeout is set to true, callers should be prepared to handle exceptions when the lock cannot be
        /// acquired in time.</remarks>
        /// <param name="lockKey">The unique identifier for the lock to acquire. This key distinguishes the lock instance within the data
        /// store.</param>
        /// <param name="waitCancelToken">A token to monitor for cancellation requests. The operation is canceled if this token is triggered.</param>
        /// <param name="slidingExpire">The duration for which the lock remains valid after each successful acquisition or renewal. The lock will
        /// automatically expire if not renewed within this period.</param>
        /// <param name="throwWhenTimeout">true to throw an exception if the lock cannot be acquired within the allowed time; otherwise, false to
        /// return null when acquisition times out.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a Lock object if the lock is
        /// successfully acquired; otherwise, null.</returns>
        Task<DataStoreLock?> AcquireLockAsync(string lockKey, CancellationToken waitCancelToken, TimeSpan slidingExpire, bool throwWhenTimeout);
    }
}
