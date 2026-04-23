#if NET48
using System.Configuration;

namespace Nuve.DataStore.Configuration;

internal class ProviderConfigurationElement : ConfigurationElement
{
    [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
    public string Name
    {
        get
        {
            return (string)this["name"];
        }
        set
        {
            this["name"] = value;
        }
    }

    [ConfigurationProperty("connectionString", IsRequired = true, IsKey = true)]
    public string ConnectionString
    {
        get
        {
            return (string)this["connectionString"];
        }
        set
        {
            this["provider"] = value;
        }
    }

    [ConfigurationProperty("connectionMode", IsRequired = true, IsKey = true)]
    public ConnectionMode ConnectionMode
    {
        get
        {
            return (ConnectionMode)this["connectionMode"];
        }
        set
        {
            this["connectionMode"] = value;
        }
    }

    [ConfigurationProperty("retryCount", IsRequired = false, IsKey = true)]
    public int? RetryCount
    {
        get
        {
            return (int?)this["retryCount"];
        }
        set
        {
            this["retryCount"] = value;
        }
    }

    [ConfigurationProperty("maxPoolSize", IsRequired = false, IsKey = true)]
    public int? MaxPoolSize
    {
        get
        {
            return (int?)this["maxPoolSize"];
        }
        set
        {
            this["maxPoolSize"] = value;
        }
    }

    [ConfigurationProperty("poolWaitTimeout", IsRequired = false, IsKey = true)]
    public TimeSpan? PoolWaitTimeout
    {
        get => (TimeSpan?)this["poolWaitTimeout"];
        set => this["poolWaitTimeout"] = value;
    }

    [ConfigurationProperty("backgroundProbeMinInterval", IsRequired = false, IsKey = true)]
    public TimeSpan? BackgroundProbeMinInterval
    {
        get => (TimeSpan?)this["backgroundProbeMinInterval"];
        set => this["backgroundProbeMinInterval"] = value;
    }

    [ConfigurationProperty("healthCheckTimeout", IsRequired = false, IsKey = true)]
    public TimeSpan? HealthCheckTimeout
    {
        get => (TimeSpan?)this["healthCheckTimeout"];
        set => this["healthCheckTimeout"] = value;
    }

    [ConfigurationProperty("swapDisposeDelay", IsRequired = false, IsKey = true)]
    public TimeSpan? SwapDisposeDelay
    {
        get => (TimeSpan?)this["swapDisposeDelay"];
        set => this["swapDisposeDelay"] = value;
    }
}
#endif