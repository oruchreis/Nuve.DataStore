using Microsoft.Extensions.Logging;

namespace Nuve.DataStore.Internal;

internal sealed class DataStoreRegistrationStore
{
    private readonly Dictionary<string, DataStoreProviderRegistration> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, DataStoreProviderOptionsRegistration> _providerOptionsFromConfiguration =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, DataStoreConnectionRegistration> _connections =
        new(StringComparer.OrdinalIgnoreCase);

    private string? _defaultConnectionName;

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
            if (!existing.FromConfiguration && !registration.FromConfiguration && throwIfAlreadyRegisteredFromCode)
            {
                throw new InvalidOperationException(
                    $"The data store provider '{registration.Name}' has already been registered.");
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

    public void AddOrReplaceProviderOptionsFromConfiguration(
        string providerName,
        ConnectionOptions options,
        ILogger logger)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(providerName);
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(logger);

        if (_providerOptionsFromConfiguration.ContainsKey(providerName))
        {
            logger.LogWarning(
                "The data store provider options for '{ProviderName}' defined in configuration are being overridden by a later configuration entry.",
                providerName);
        }

        _providerOptionsFromConfiguration[providerName] = new DataStoreProviderOptionsRegistration
        {
            Name = providerName,
            Options = options,
            FromConfiguration = true
        };
    }

    public IReadOnlyCollection<DataStoreProviderRegistration> GetFinalProviders(ILogger logger)
    {
        ThrowHelper.ThrowIfNull(logger);

        var result = new List<DataStoreProviderRegistration>(_providers.Count);

        foreach (var provider in _providers.Values)
        {
            var finalOptions = provider.Options;

            if (_providerOptionsFromConfiguration.TryGetValue(provider.Name, out var configOptionsRegistration))
            {
                finalOptions = configOptionsRegistration.Options;

                if (provider.HasExplicitOptions)
                {
                    logger.LogWarning(
                        "The data store provider '{ProviderName}' defined in code overrides provider options loaded from configuration.",
                        provider.Name);

                    finalOptions = provider.Options;
                }
            }

            result.Add(new DataStoreProviderRegistration
            {
                Name = provider.Name,
                ProviderType = provider.ProviderType,
                Options = finalOptions,
                HasExplicitOptions = provider.HasExplicitOptions,
                FromConfiguration = provider.FromConfiguration
            });
        }

        return result;
    }

    public void AddOrReplaceConnection(
        DataStoreConnectionRegistration registration,
        ILogger logger,
        bool throwIfAlreadyRegisteredFromCode)
    {
        ThrowHelper.ThrowIfNull(registration);
        ThrowHelper.ThrowIfNull(logger);

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
            return;
        }

        _connections.Add(registration.Name, registration);
    }
}