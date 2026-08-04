using System;
using StackExchange.Redis;

namespace Nuve.DataStore.Redis;

/// <summary>
/// Server side capabilities that decide which command variant the provider may use.
/// They are resolved once per connection, because the server cannot change underneath a live connection.
/// A deployment may run different servers per environment — for example Redis 7.x in production and
/// KeyDB 6 in development — so the provider picks the native command where it exists and a script otherwise.
/// </summary>
public sealed class RedisServerCapabilities
{
    /// <summary>
    /// EXPIRE and PEXPIRE gained the NX, XX, GT and LT options in Redis 7.0.
    /// </summary>
    private static readonly Version ExpireOptionsMinimumVersion = new(7, 0);

    internal RedisServerCapabilities(
        Version serverVersion,
        ServerType serverType,
        bool scripting,
        bool setKeepTtl,
        bool crossSlotMultiKey)
    {
        ServerVersion = serverVersion;
        ServerType = serverType;
        SupportsScripting = scripting;
        SupportsSetKeepTtl = setKeepTtl;
        SupportsCrossSlotMultiKey = crossSlotMultiKey;
        SupportsExpireOptions = serverVersion >= ExpireOptionsMinimumVersion;
    }

    /// <summary>
    /// Whether a single multi-key write may span several slots. It is measured rather than derived,
    /// because the topology cannot be told from the endpoint alone: a proxy in front of a sharded
    /// database looks standalone, and among those some accept a cross-slot write while others reject it.
    /// When false the provider keeps the atomicity of each slot but not of the call as a whole.
    /// </summary>
    public bool SupportsCrossSlotMultiKey { get; }

    /// <summary>
    /// How the endpoint presents itself. A proxy that fronts a sharded database reports
    /// <see cref="StackExchange.Redis.ServerType.Standalone"/>, so this separates a client side
    /// cluster from a single endpoint, but not a true standalone from a proxied set of shards.
    /// </summary>
    public ServerType ServerType { get; }

    /// <summary>
    /// Whether the client speaks the cluster protocol and therefore has to keep every key of a
    /// multi-key command on one slot itself.
    /// </summary>
    public bool IsClustered => ServerType == ServerType.Cluster;

    /// <summary>
    /// The version reported by the server. Forks report their own numbering, so a fork built on an
    /// older Redis reports that fork's version rather than the Redis release it derives from.
    /// </summary>
    public Version ServerVersion { get; }

    /// <summary>
    /// Whether the expiration can be applied conditionally with <c>PEXPIRE key ttl NX</c>
    /// instead of the equivalent script.
    /// </summary>
    public bool SupportsExpireOptions { get; }

    /// <summary>
    /// Whether the server evaluates Lua scripts, which is the portable path for conditional commands.
    /// </summary>
    public bool SupportsScripting { get; }

    /// <summary>
    /// Whether <c>SET key value KEEPTTL</c> is available.
    /// </summary>
    public bool SupportsSetKeepTtl { get; }

    /// <summary>
    /// Whether these capabilities came from a server answer rather than from the conservative default.
    /// </summary>
    public bool IsResolved => ServerVersion > EmptyVersion;

    private static readonly Version EmptyVersion = new(0, 0);

    /// <summary>
    /// Used until the server answers and whenever it cannot be reached. Every optional command counts
    /// as missing so the portable path is taken; scripting is assumed because it predates every
    /// server this provider supports and is the fallback the other decisions rely on.
    /// </summary>
    internal static RedisServerCapabilities Unknown { get; } =
        new(EmptyVersion, ServerType.Standalone, scripting: true, setKeepTtl: false, crossSlotMultiKey: false);
}
