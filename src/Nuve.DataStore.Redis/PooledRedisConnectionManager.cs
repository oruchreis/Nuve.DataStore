using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Nuve.DataStore.Redis;

internal sealed class PooledRedisConnectionManager : IRedisConnectionManager
{
    private readonly string _connectionString;
    private readonly TimeSpan _poolWaitTimeout;
    private readonly ConcurrentQueue<ConnectionMultiplexer> _pool = new();
    private readonly SemaphoreSlim _slotSemaphore;

    public PooledRedisConnectionManager(ConnectionOptions options)
    {
        _connectionString = options.ConnectionString;
        _poolWaitTimeout = options.PoolWaitTimeout;
        _slotSemaphore = new SemaphoreSlim(options.MaxPoolSize, options.MaxPoolSize);
    }

    public IRedisConnectionLease Acquire()
    {
        if (!_slotSemaphore.Wait(_poolWaitTimeout))
            throw new TimeoutException("Redis connection pool exhausted.");

        try
        {
            if (_pool.TryDequeue(out var existing))
                return new PooledRedisConnectionLease(this, existing);

            var created = CreateMultiplexer();
            return new PooledRedisConnectionLease(this, created);
        }
        catch
        {
            _slotSemaphore.Release();
            throw;
        }
    }

    public async ValueTask<IRedisConnectionLease> AcquireAsync()
    {
        if (!await _slotSemaphore.WaitAsync(_poolWaitTimeout).ConfigureAwait(false))
            throw new TimeoutException("Redis connection pool exhausted.");

        try
        {
            if (_pool.TryDequeue(out var existing))
                return new PooledRedisConnectionLease(this, existing);

            var created = await CreateMultiplexerAsync().ConfigureAwait(false);
            return new PooledRedisConnectionLease(this, created);
        }
        catch
        {
            _slotSemaphore.Release();
            throw;
        }
    }

    public void ReportTimeout(ConnectionMultiplexer multiplexer, Exception exception)
    {
        // Pooled modda agresif davranmak istenirse lease dispose anında discard edilir.
    }

    public void ReportConnectionFailure(ConnectionMultiplexer multiplexer, Exception exception)
    {
        // Pooled modda agresif davranmak istenirse lease dispose anında discard edilir.
    }

    internal void Return(ConnectionMultiplexer multiplexer)
    {
        _pool.Enqueue(multiplexer);
        _slotSemaphore.Release();
    }

    internal void Discard(ConnectionMultiplexer multiplexer)
    {
        try
        {
            multiplexer.Dispose();
        }
        finally
        {
            _slotSemaphore.Release();
        }
    }

    private ConnectionMultiplexer CreateMultiplexer()
    {
        var options = ConfigurationOptions.Parse(_connectionString);
        options.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(options);
    }

    private async Task<ConnectionMultiplexer> CreateMultiplexerAsync()
    {
        var options = ConfigurationOptions.Parse(_connectionString);
        options.AbortOnConnectFail = false;
        return await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
    }

    public void Dispose()
    {
        while (_pool.TryDequeue(out var mux))
        {
            try
            {
                mux.Dispose();
            }
            catch
            {
            }
        }

        _slotSemaphore.Dispose();
    }

    public ValueTask DisposeAsync()
    {
#if NET6_0_OR_GREATER
        Dispose();
        return ValueTask.CompletedTask;
#else
        Dispose();
        return new ValueTask(Task.CompletedTask);
#endif
    }

    private sealed class PooledRedisConnectionLease : IRedisConnectionLease
    {
        private readonly PooledRedisConnectionManager _owner;
        private int _disposed;

        public ConnectionMultiplexer Multiplexer { get; }

        public PooledRedisConnectionLease(PooledRedisConnectionManager owner, ConnectionMultiplexer multiplexer)
        {
            _owner = owner;
            Multiplexer = multiplexer;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            if (Multiplexer.IsConnected)
                _owner.Return(Multiplexer);
            else
                _owner.Discard(Multiplexer);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
#if NET6_0_OR_GREATER
            return ValueTask.CompletedTask;
#else
            return new ValueTask(Task.CompletedTask);
#endif
        }
    }
}