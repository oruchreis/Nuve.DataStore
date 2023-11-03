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
    public string ProviderName
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
}
#endif