using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Nuve.DataStore.Redis;


internal interface IRedisConnectionLease : IDisposable, IAsyncDisposable
{
    ConnectionMultiplexer Multiplexer { get; }
}

internal interface IRedisConnectionManager : IDisposable, IAsyncDisposable
{
    IRedisConnectionLease Acquire();
    ValueTask<IRedisConnectionLease> AcquireAsync();

    void ReportTimeout(ConnectionMultiplexer multiplexer, Exception exception);
    void ReportConnectionFailure(ConnectionMultiplexer multiplexer, Exception exception);
}
