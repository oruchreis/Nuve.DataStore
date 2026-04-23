using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nuve.DataStore.Internal;

#if  TEST
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Nuve.DataStore.Test")]
#endif

namespace Nuve.DataStore;

public sealed class DataStoreManager
{
    private static readonly object SyncInitializingSentinel = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyList<DataStoreProviderRegistration> _providerRegistrations;
    private readonly Dictionary<string, DataStoreConnectionRegistration> _connections;
    private readonly string? _defaultConnectionName;

    // null                               => not initialized
    // SyncInitializingSentinel           => sync initialization in progress
    // Task<Dictionary<string, provider>> => async initialization in progress
    // Dictionary<string, provider>       => initialized
    // ExceptionDispatchInfo              => faulted
    private object? _providerState;

    internal DataStoreManager(
        IServiceProvider serviceProvider,
        IEnumerable<DataStoreProviderRegistration> providerRegistrations,
        IEnumerable<DataStoreConnectionRegistration> connectionRegistrations,
        IDataStoreSerializer defaultSerializer,
        IDataStoreCompressor defaultCompressor,
        IDataStoreProfiler globalProfiler,
        ILogger<DataStoreManager> logger)
    {
        ThrowHelper.ThrowIfNull(serviceProvider);
        ThrowHelper.ThrowIfNull(providerRegistrations);
        ThrowHelper.ThrowIfNull(connectionRegistrations);
        ThrowHelper.ThrowIfNull(defaultSerializer);
        ThrowHelper.ThrowIfNull(defaultCompressor);
        ThrowHelper.ThrowIfNull(globalProfiler);
        ThrowHelper.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        DefaultSerializer = defaultSerializer;
        DefaultCompressor = defaultCompressor;
        GlobalProfiler = globalProfiler;
        Logger = logger;

        _providerRegistrations = providerRegistrations.ToArray();

        var connectionArray = connectionRegistrations.ToArray();

        _connections = connectionArray.ToDictionary(
            x => x.Name,
            x => x,
            StringComparer.OrdinalIgnoreCase);

        foreach (var connection in connectionArray)
        {
            if (_providerRegistrations.All(x => !string.Equals(x.Name, connection.ProviderName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"The data store connection '{connection.Name}' references provider '{connection.ProviderName}', but no provider with that name has been registered.");
            }
        }

        _defaultConnectionName = connectionArray.FirstOrDefault(x => x.IsDefault)?.Name;
    }

    internal ILogger<DataStoreManager> Logger { get; }

    public IDataStoreSerializer DefaultSerializer { get; }

    public IDataStoreCompressor DefaultCompressor { get; }

    public IDataStoreProfiler? GlobalProfiler { get; }

    public bool IsInitialized => Volatile.Read(ref _providerState) is Dictionary<string, IDataStoreProvider>;

    public void InitializeProviders()
    {
        _ = GetProvidersSync();
    }

    public Task InitializeProvidersAsync()
    {
        return GetProvidersAsync();
    }

    internal DataStoreConnectionContext GetConnection(string? connectionName)
    {
        var providers = GetProvidersAlreadyInitialized();
        var registration = GetConnectionRegistration(connectionName);

        return new DataStoreConnectionContext(
            providers[registration.ProviderName],
            registration.RootNamespace,
            registration.CompressBiggerThan);
    }

    private Dictionary<string, IDataStoreProvider> GetProvidersAlreadyInitialized()
    {
        var state = Volatile.Read(ref _providerState);

        if (state is Dictionary<string, IDataStoreProvider> readyProviders)
            return readyProviders;

        if (state is ExceptionDispatchInfo capturedException)
            capturedException.Throw();

        throw new InvalidOperationException(
            "The data store providers have not been initialized. Call InitializeDataStore() or InitializeDataStoreAsync() before creating or using data store instances.");
    }

    private Dictionary<string, IDataStoreProvider> GetProvidersSync()
    {
        while (true)
        {
            var state = Volatile.Read(ref _providerState);

            if (state is Dictionary<string, IDataStoreProvider> readyProviders)
                return readyProviders;

            if (state is Task<Dictionary<string, IDataStoreProvider>> initializationTask)
                return initializationTask.GetAwaiter().GetResult();

            if (state is ExceptionDispatchInfo capturedException)
                capturedException.Throw();

            if (state == null)
            {
                if (Interlocked.CompareExchange(ref _providerState, SyncInitializingSentinel, null) == null)
                {
                    try
                    {
                        var providers = BuildProvidersSync();
                        Volatile.Write(ref _providerState, providers);
                        return providers;
                    }
                    catch (Exception ex)
                    {
                        var captured = ExceptionDispatchInfo.Capture(ex);
                        Volatile.Write(ref _providerState, captured);
                        captured.Throw();
                    }
                }

                continue;
            }

            if (ReferenceEquals(state, SyncInitializingSentinel))
            {
                Thread.SpinWait(1);
                continue;
            }
        }
    }

    private Task GetProvidersAsync()
    {
        while (true)
        {
            var state = Volatile.Read(ref _providerState);

            if (state is Dictionary<string, IDataStoreProvider>)
                return Task.CompletedTask;

            if (state is Task<Dictionary<string, IDataStoreProvider>> task)
                return task;

            if (state is ExceptionDispatchInfo capturedException)
                capturedException.Throw();

            if (state == null)
            {
                var initializationTask = BuildProvidersAsyncCore();

                if (Interlocked.CompareExchange(ref _providerState, initializationTask, null) == null)
                    return initializationTask;

                continue;
            }

            if (ReferenceEquals(state, SyncInitializingSentinel))
            {
                Thread.SpinWait(1);
                continue;
            }
        }
    }

    private Dictionary<string, IDataStoreProvider> BuildProvidersSync()
    {
        var providers = new Dictionary<string, IDataStoreProvider>(StringComparer.OrdinalIgnoreCase);

        foreach (var registration in _providerRegistrations)
        {
            var provider = (IDataStoreProvider)ActivatorUtilities.CreateInstance(
                _serviceProvider,
                registration.ProviderType);

            provider.Initialize(registration.Options, GlobalProfiler);
            providers.Add(registration.Name, provider);
        }

        return providers;
    }

    private async Task<Dictionary<string, IDataStoreProvider>> BuildProvidersAsyncCore()
    {
        try
        {
            var providers = new Dictionary<string, IDataStoreProvider>(StringComparer.OrdinalIgnoreCase);

            foreach (var registration in _providerRegistrations)
            {
                var provider = (IDataStoreProvider)ActivatorUtilities.CreateInstance(
                    _serviceProvider,
                    registration.ProviderType);

                await provider.InitializeAsync(registration.Options, GlobalProfiler).ConfigureAwait(false);
                providers.Add(registration.Name, provider);
            }

            Volatile.Write(ref _providerState, providers);
            return providers;
        }
        catch (Exception ex)
        {
            var captured = ExceptionDispatchInfo.Capture(ex);
            Volatile.Write(ref _providerState, captured);
            captured.Throw();
            throw;
        }
    }

    private DataStoreConnectionRegistration GetConnectionRegistration(string? connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
        {
            connectionName = _defaultConnectionName
                ?? throw new InvalidOperationException(
                    "No default data store connection has been configured.");
        }

        if (!_connections.TryGetValue(connectionName, out var registration))
        {
            throw new InvalidOperationException(
                $"The data store connection '{connectionName}' could not be found.");
        }

        return registration;
    }
}