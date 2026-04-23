using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nuve.DataStore.Internal;

namespace Nuve.DataStore;

public static class DataStoreBuilderExtensions
{
    private const string DefaultConnectionName = "__default__";

    public static IDataStoreBuilder AddDefaultConnection(
        this IDataStoreBuilder builder,
        string provider,
        string? rootNamespace = null,
        int? compressBiggerThan = null)
    {
        ThrowHelper.ThrowIfNull(builder);

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);
        var logger = NullLogger.Instance;

        registrationStore.AddOrReplaceConnection(
            new DataStoreConnectionRegistration
            {
                Name = DefaultConnectionName,
                ProviderName = provider,
                RootNamespace = rootNamespace ?? string.Empty,
                CompressBiggerThan = compressBiggerThan,
                IsDefault = true,
                FromConfiguration = false
            },
            logger,
            throwIfAlreadyRegisteredFromCode: true);

        return builder;
    }

    public static IDataStoreBuilder AddConnection(
        this IDataStoreBuilder builder,
        string name,
        string provider,
        string? rootNamespace = null,
        int? compressBiggerThan = null)
    {
        ThrowHelper.ThrowIfNull(builder);

        var registrationStore = DataStoreServiceCollectionExtensions.GetOrAddRegistrationStore(builder.Services);
        var logger = NullLogger.Instance;

        registrationStore.AddOrReplaceConnection(
            new DataStoreConnectionRegistration
            {
                Name = name,
                ProviderName = provider,
                RootNamespace = rootNamespace ?? string.Empty,
                CompressBiggerThan = compressBiggerThan,
                IsDefault = false,
                FromConfiguration = false
            },
            logger,
            throwIfAlreadyRegisteredFromCode: true);

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
}