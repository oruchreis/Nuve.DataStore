using StackExchange.Redis;
using System.Net.Sockets;

namespace Nuve.DataStore.Redis;

internal sealed class SharedRedisConnectionManager : IRedisConnectionManager
{
    private readonly string _connectionString;
    private readonly TimeSpan _backgroundProbeMinInterval;
    private readonly TimeSpan _healthCheckTimeout;
    private readonly TimeSpan _swapDisposeDelay;

    private volatile ConnectionMultiplexer? _shared;
    private int _backgroundProbeScheduled;
    private long _lastProbeTicks;

    public SharedRedisConnectionManager(ConnectionOptions options)
    {
        _connectionString = options.ConnectionString;
        _backgroundProbeMinInterval = options.BackgroundProbeMinInterval;
        _healthCheckTimeout = options.HealthCheckTimeout;
        _swapDisposeDelay = options.SwapDisposeDelay;
    }

    public IRedisConnectionLease Acquire()
    {
        var mux = GetOrCreateShared();
        return new SharedRedisConnectionLease(mux);
    }

    public async ValueTask<IRedisConnectionLease> AcquireAsync()
    {
        var mux = _shared;
        if (mux == null)
        {
            var created = await CreateMultiplexerAsync().ConfigureAwait(false);
            WireEvents(created);

            var previous = Interlocked.CompareExchange(ref _shared, created, null);
            if (previous != null)
            {
                created.Dispose();
                mux = previous;
            }
            else
            {
                mux = created;
            }
        }

        return new SharedRedisConnectionLease(mux);
    }

    public void ReportTimeout(ConnectionMultiplexer multiplexer, Exception exception)
    {
        // Shared mode için timeout tek başına reconnect nedeni değil.
        // StackExchange.Redis kendi reconnect mekanizmasını çalıştıracaktır.
    }

    public void ReportConnectionFailure(ConnectionMultiplexer multiplexer, Exception exception)
    {
        ScheduleBackgroundProbe();
    }

    private ConnectionMultiplexer GetOrCreateShared()
    {
        var mux = _shared;
        if (mux != null)
            return mux;

        var created = CreateMultiplexer();
        WireEvents(created);

        var previous = Interlocked.CompareExchange(ref _shared, created, null);
        if (previous != null)
        {
            created.Dispose();
            return previous;
        }

        return created;
    }

    private void ScheduleBackgroundProbe()
    {
        var nowTicks = DateTime.UtcNow.Ticks;
        var lastTicks = Interlocked.Read(ref _lastProbeTicks);

        if (nowTicks - lastTicks < _backgroundProbeMinInterval.Ticks)
            return;

        Interlocked.Exchange(ref _lastProbeTicks, nowTicks);

        if (Interlocked.CompareExchange(ref _backgroundProbeScheduled, 1, 0) != 0)
            return;

        _ = Task.Run(ProbeAndMaybeReconnectAsync);
    }

    private async Task ProbeAndMaybeReconnectAsync()
    {
        try
        {
            var current = _shared;
            if (current == null)
                return;

            if (await IsHealthyAsync(current).ConfigureAwait(false))
                return;

            var replacement = await CreateMultiplexerAsync().ConfigureAwait(false);
            WireEvents(replacement);

            var old = Interlocked.Exchange(ref _shared, replacement);
            if (old != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(_swapDisposeDelay).ConfigureAwait(false);
                        old.Dispose();
                    }
                    catch
                    {
                    }
                });
            }
        }
        catch
        {
        }
        finally
        {
            Volatile.Write(ref _backgroundProbeScheduled, 0);
        }
    }

    private async Task<bool> IsHealthyAsync(ConnectionMultiplexer mux)
    {
        try
        {
            if (!mux.IsConnected)
                return false;

            var db = mux.GetDatabase();
            var pingTask = db.PingAsync();
            var completed = await Task.WhenAny(pingTask, Task.Delay(_healthCheckTimeout)).ConfigureAwait(false);

            if (completed != pingTask)
                return false;

            _ = await pingTask.ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
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

    private void WireEvents(ConnectionMultiplexer mux)
    {
        mux.ConnectionFailed += (_, args) =>
        {
            ReportConnectionFailure(
                mux,
                new RedisConnectionException(args.FailureType, args.Exception?.Message ?? "Redis connection failed.", args.Exception)
            );
        };

        mux.ErrorMessage += (_, _) =>
        {
            // İstenirse log eklenebilir.
        };

        mux.InternalError += (_, args) =>
        {
            if (args.Exception is RedisConnectionException or SocketException)
                ScheduleBackgroundProbe();
        };
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _shared, null)?.Dispose();
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

    private sealed class SharedRedisConnectionLease : IRedisConnectionLease
    {
        public ConnectionMultiplexer Multiplexer { get; }

        public SharedRedisConnectionLease(ConnectionMultiplexer multiplexer)
        {
            Multiplexer = multiplexer;
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
#if NET6_0_OR_GREATER
            return ValueTask.CompletedTask;
#else
            return new ValueTask(Task.CompletedTask);
#endif
        }
    }
}