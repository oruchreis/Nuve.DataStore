#if NET48
using System.Configuration;

namespace Nuve.DataStore.Configuration;

internal class ConnectionConfigurationElement : ConfigurationElement
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

    [ConfigurationProperty("provider", IsRequired = true, IsKey = true)]
    public string Provider
    {
        get
        {
            return (string)this["provider"];
        }
        set
        {
            this["provider"] = value;
        }
    }

    [ConfigurationProperty("serializer", IsRequired = false, IsKey = true)]
    public string Serializer
    {
        get
        {
            return (string)this["serializer"];
        }
        set
        {
            this["serializer"] = value;
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
            this["connectionString"] = value;
        }
    }

    [ConfigurationProperty("namespace", IsRequired = false, IsKey = true)]
    public string Namespace
    {
        get
        {
            return (string)this["namespace"];
        }
        set
        {
            this["namespace"] = value;
        }
    }

    [ConfigurationProperty("default", IsRequired = false, IsKey = true)]
    public bool IsDefault
    {
        get
        {
            return ((bool?)this["default"]) ?? false;
        }
        set
        {
            this["default"] = value;
        }
    }

    [ConfigurationProperty("compressBiggerThan", IsRequired = false, IsKey = true)]
    public int? CompressBiggerThan
    {
        get => (int?)this["compressBiggerThan"];
        set => this["compressBiggerThan"] = value;
    }

    [ConfigurationProperty("connectionMode", IsRequired = false, IsKey = true)]
    public ConnectionMode ConnectionMode
    {
        get => (ConnectionMode?)this["connectionMode"] ?? ConnectionMode.Shared;
        set => this["connectionMode"] = value;
    }

    [ConfigurationProperty("retryCount", IsRequired = false, IsKey = true)]
    public int? RetryCount
    {
        get => (int?)this["retryCount"];
        set => this["retryCount"] = value;
    }

    [ConfigurationProperty("maxPoolSize", IsRequired = false, IsKey = true)]
    public int? MaxPoolSize
    {
        get => (int?)this["maxPoolSize"];
        set => this["maxPoolSize"] = value;
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
