using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Nuve.DataStore.Internal;

namespace Nuve.DataStore;

public static class DataStoreBuilderExtensions
{
    public static IDataStoreBuilder AddDefaultConnection(
        this IDataStoreBuilder builder,
        string provider,
        ConnectionOptions options,
        string? serializer = null,
        string? rootNamespace = null,
        int? compressBiggerThan = null)
    {
        ThrowHelper.ThrowIfNull(builder);

        return builder.AddConnection(
            DataStoreConstants.DefaultConnectionName,
            provider,
            options,
            serializer,
            rootNamespace,
            compressBiggerThan,
            isDefault: true);
    }

    public static IDataStoreBuilder AddDefaultConnection(
        this IDataStoreBuilder builder,
        string provider,
        Action<ConnectionOptions> configure,
        string? serializer = null,
        string? rootNamespace = null,
        int? compressBiggerThan = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configure);

        return builder.AddConnection(
            DataStoreConstants.DefaultConnectionName,
            provider,
            configure,
            serializer,
            rootNamespace,
            compressBiggerThan,
            isDefault: true);
    }

    public static IDataStoreBuilder AddConnection(
        this IDataStoreBuilder builder,
        string name,
        string provider,
        ConnectionOptions options,
        string? serializer = null,
        string? rootNamespace = null,
        int? compressBiggerThan = null,
        bool isDefault = false)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(options);

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);

        registrationStore.AddOrReplaceConnection(
            new DataStoreConnectionRegistration
            {
                Name = isDefault ? DataStoreConstants.DefaultConnectionName : name,
                ProviderName = provider,
                Options = CloneConnectionOptions(options),
                SerializerName = serializer,
                RootNamespace = rootNamespace ?? string.Empty,
                CompressBiggerThan = compressBiggerThan,
                IsDefault = isDefault,
                FromConfiguration = false
            },
            NullLogger.Instance,
            throwIfAlreadyRegisteredFromCode: false);

        return builder;
    }

    public static IDataStoreBuilder AddConnection(
        this IDataStoreBuilder builder,
        string name,
        string provider,
        Action<ConnectionOptions> configure,
        string? serializer = null,
        string? rootNamespace = null,
        int? compressBiggerThan = null,
        bool isDefault = false)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configure);

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);
        var normalizedName = isDefault ? DataStoreConstants.DefaultConnectionName : name;
        var options = registrationStore.TryGetConnection(normalizedName, out var existingRegistration)
            ? CloneConnectionOptions(existingRegistration.Options)
            : new ConnectionOptions();

        configure(options);

        registrationStore.AddOrReplaceConnection(
            new DataStoreConnectionRegistration
            {
                Name = normalizedName,
                ProviderName = provider,
                Options = options,
                SerializerName = serializer ?? existingRegistration?.SerializerName,
                RootNamespace = rootNamespace ?? existingRegistration?.RootNamespace ?? string.Empty,
                CompressBiggerThan = compressBiggerThan ?? existingRegistration?.CompressBiggerThan,
                IsDefault = isDefault,
                FromConfiguration = false
            },
            NullLogger.Instance,
            throwIfAlreadyRegisteredFromCode: false);

        return builder;
    }

    public static IDataStoreBuilder AddDataStoreSerializer<TSerializer>(
        this IDataStoreBuilder builder)
        where TSerializer : class, IDataStoreSerializer
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.Replace(
            ServiceDescriptor.Singleton<IDataStoreSerializer, TSerializer>());

        return builder;
    }

    public static IDataStoreBuilder AddDataStoreSerializer<TSerializer>(
        this IDataStoreBuilder builder,
        string serializerName)
        where TSerializer : class, IDataStoreSerializer
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(serializerName);

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);
        registrationStore.AddSerializer(
            new DataStoreSerializerRegistration
            {
                Name = serializerName,
                SerializerType = typeof(TSerializer)
            },
            NullLogger.Instance);

        return builder;
    }

    public static IDataStoreBuilder AddDataStoreSerializer(
        this IDataStoreBuilder builder,
        IDataStoreSerializer serializer)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(serializer);

        builder.Services.Replace(
            ServiceDescriptor.Singleton(typeof(IDataStoreSerializer), serializer));

        return builder;
    }

    public static IDataStoreBuilder AddDataStoreSerializer(
        this IDataStoreBuilder builder,
        string serializerName,
        IDataStoreSerializer serializer)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(serializerName);
        ThrowHelper.ThrowIfNull(serializer);

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);
        registrationStore.AddSerializer(
            new DataStoreSerializerRegistration
            {
                Name = serializerName,
                SerializerInstance = serializer
            },
            NullLogger.Instance);

        return builder;
    }

    public static IDataStoreBuilder AddDataStoreCompressor<TCompressor>(
        this IDataStoreBuilder builder)
        where TCompressor : class, IDataStoreCompressor
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.Replace(
            ServiceDescriptor.Singleton<IDataStoreCompressor, TCompressor>());

        return builder;
    }

    public static IDataStoreBuilder AddDataStoreCompressor(
        this IDataStoreBuilder builder,
        IDataStoreCompressor compressor)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(compressor);

        builder.Services.Replace(
            ServiceDescriptor.Singleton(typeof(IDataStoreCompressor), compressor));

        return builder;
    }

    private static ConnectionOptions CloneConnectionOptions(ConnectionOptions options)
    {
        return new ConnectionOptions
        {
            ConnectionString = options.ConnectionString,
            ConnectionMode = options.ConnectionMode,
            RetryCount = options.RetryCount,
            MaxPoolSize = options.MaxPoolSize,
            PoolWaitTimeout = options.PoolWaitTimeout,
            BackgroundProbeMinInterval = options.BackgroundProbeMinInterval,
            HealthCheckTimeout = options.HealthCheckTimeout,
            SwapDisposeDelay = options.SwapDisposeDelay
        };
    }
}
