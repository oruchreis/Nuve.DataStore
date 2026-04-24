using Microsoft.Extensions.Logging;

namespace Nuve.DataStore.Internal;

internal sealed class DataStoreRegistrationStore
{
    private readonly Dictionary<string, DataStoreProviderRegistration> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, DataStoreSerializerRegistration> _serializers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, DataStoreConnectionRegistration> _connections =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<DataStoreConnectionRegistration> _connectionMutations = [];

    private string? _defaultConnectionName;

    public IReadOnlyCollection<DataStoreProviderRegistration> Providers => _providers.Values;

    public IReadOnlyCollection<DataStoreSerializerRegistration> Serializers => _serializers.Values;

    public IReadOnlyCollection<DataStoreConnectionRegistration> Connections => _connections.Values;

    public string? DefaultConnectionName => _defaultConnectionName;

    public void AddOrReplaceProvider(
        DataStoreProviderRegistration registration,
        ILogger logger,
        bool throwIfAlreadyRegisteredFromCode)
    {
        ThrowHelper.ThrowIfNull(registration);
        ThrowHelper.ThrowIfNull(logger);

        if (_providers.TryGetValue(registration.Name, out var existing))
        {
            if (!existing.FromConfiguration && !registration.FromConfiguration)
            {
                logger.LogWarning(
                    "The data store provider '{ProviderName}' has already been registered. The existing registration will be kept.",
                    registration.Name);
                return;
            }

            if (existing.FromConfiguration && !registration.FromConfiguration)
            {
                logger.LogWarning(
                    "The data store provider '{ProviderName}' defined in code overrides the existing configuration entry.",
                    registration.Name);
            }

            _providers[registration.Name] = registration;
            return;
        }

        _providers.Add(registration.Name, registration);
    }

    public void AddOrReplaceConnection(
        DataStoreConnectionRegistration registration,
        ILogger logger,
        bool throwIfAlreadyRegisteredFromCode)
    {
        ThrowHelper.ThrowIfNull(registration);
        ThrowHelper.ThrowIfNull(logger);

        ThrowHelper.ThrowIfNullOrWhiteSpace(registration.ProviderName);

        if (registration.ConfigureOptions == null)
        {
            ThrowHelper.ThrowIfNull(registration.Options);
            ThrowHelper.ThrowIfNullOrWhiteSpace(registration.Options.ConnectionString);
        }

        if (registration.IsDefault)
        {
            if (!string.IsNullOrWhiteSpace(_defaultConnectionName) &&
                !string.Equals(_defaultConnectionName, registration.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (!registration.FromConfiguration)
                {
                    logger.LogWarning(
                        "The default data store connection defined in code overrides the existing configuration entry '{ConnectionName}'.",
                        _defaultConnectionName);
                }
            }

            _defaultConnectionName = registration.Name;
        }

        if (_connections.TryGetValue(registration.Name, out var existing))
        {
            if (!existing.FromConfiguration && !registration.FromConfiguration && throwIfAlreadyRegisteredFromCode)
            {
                throw new InvalidOperationException(
                    $"The data store connection '{registration.Name}' has already been registered.");
            }

            if (existing.FromConfiguration && !registration.FromConfiguration)
            {
                logger.LogWarning(
                    "The data store connection '{ConnectionName}' defined in code overrides the existing configuration entry.",
                    registration.Name);
            }

            _connections[registration.Name] = registration;

            if (!registration.FromConfiguration)
                _connectionMutations.Add(registration);

            return;
        }

        _connections.Add(registration.Name, registration);

        if (!registration.FromConfiguration)
            _connectionMutations.Add(registration);
    }

    public void AddSerializer(
        DataStoreSerializerRegistration registration,
        ILogger logger)
    {
        ThrowHelper.ThrowIfNull(registration);
        ThrowHelper.ThrowIfNull(logger);
        ThrowHelper.ThrowIfNullOrWhiteSpace(registration.Name);

        if (_serializers.ContainsKey(registration.Name))
        {
            logger.LogWarning(
                "The data store serializer '{SerializerName}' has already been registered. The existing registration will be kept.",
                registration.Name);
            return;
        }

        _serializers.Add(registration.Name, registration);
    }

    public bool TryGetConnection(string name, out DataStoreConnectionRegistration registration)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);
        return _connections.TryGetValue(name, out registration!);
    }

    public bool TryGetSerializer(string name, out DataStoreSerializerRegistration registration)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(name);
        return _serializers.TryGetValue(name, out registration!);
    }

    public IReadOnlyCollection<DataStoreConnectionRegistration> CreateFinalConnections(
        IReadOnlyCollection<DataStoreConnectionRegistration> configurationConnections,
        IReadOnlyCollection<DataStoreConnectionRegistration> legacyConfigurationConnections)
    {
        var effectiveConnections = new Dictionary<string, DataStoreConnectionRegistration>(StringComparer.OrdinalIgnoreCase);

        MergeConnections(effectiveConnections, legacyConfigurationConnections);
        MergeConnections(effectiveConnections, configurationConnections);

        foreach (var registration in _connectionMutations)
        {
            effectiveConnections[registration.Name] = registration.ConfigureOptions == null
                ? CreateReplacedConnection(registration)
                : CreateMergedConnection(
                    effectiveConnections.TryGetValue(registration.Name, out var existingRegistration)
                        ? existingRegistration
                        : null,
                    registration);
        }

        foreach (var registration in effectiveConnections.Values)
        {
            ThrowHelper.ThrowIfNull(registration.Options);
            ThrowHelper.ThrowIfNullOrWhiteSpace(registration.ProviderName);
            ThrowHelper.ThrowIfNullOrWhiteSpace(registration.Options.ConnectionString);
        }

        return effectiveConnections.Values.ToArray();
    }

    private static void MergeConnections(
        Dictionary<string, DataStoreConnectionRegistration> destination,
        IEnumerable<DataStoreConnectionRegistration> source)
    {
        foreach (var registration in source)
            destination[registration.Name] = registration;
    }

    private static DataStoreConnectionRegistration CreateReplacedConnection(
        DataStoreConnectionRegistration registration)
    {
        return new DataStoreConnectionRegistration
        {
            Name = registration.Name,
            ProviderName = registration.ProviderName,
            Options = CloneConnectionOptions(registration.Options),
            SerializerName = registration.SerializerName,
            RootNamespace = registration.RootNamespace ?? string.Empty,
            CompressBiggerThan = registration.CompressBiggerThan,
            IsDefault = registration.IsDefault,
            FromConfiguration = false
        };
    }

    private static DataStoreConnectionRegistration CreateMergedConnection(
        DataStoreConnectionRegistration? existingRegistration,
        DataStoreConnectionRegistration registration)
    {
        var options = existingRegistration != null
            ? CloneConnectionOptions(existingRegistration.Options)
            : new ConnectionOptions();

        registration.ConfigureOptions!(options);

        return new DataStoreConnectionRegistration
        {
            Name = registration.Name,
            ProviderName = registration.ProviderName,
            Options = options,
            SerializerName = registration.SerializerName ?? existingRegistration?.SerializerName,
            RootNamespace = registration.RootNamespace ?? existingRegistration?.RootNamespace ?? string.Empty,
            CompressBiggerThan = registration.CompressBiggerThan ?? existingRegistration?.CompressBiggerThan,
            IsDefault = registration.IsDefault,
            FromConfiguration = false
        };
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
