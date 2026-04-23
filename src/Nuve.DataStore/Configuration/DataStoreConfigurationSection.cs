#if NET48
using System.Configuration;

namespace Nuve.DataStore.Configuration;

internal class DataStoreConfigurationSection: ConfigurationSection
{
    private const string ConfigPath = "dataStore";

    public static DataStoreConfigurationSection GetConfiguration()
    {
        return (DataStoreConfigurationSection)ConfigurationManager.GetSection(ConfigPath);
    }

    [ConfigurationProperty("providers")]
    [ConfigurationCollection(typeof(ProviderConfigurationCollection),
        AddItemName = "add",
        ClearItemsName = "clear",
        RemoveItemName = "remove")]
    public ProviderConfigurationCollection Providers
    {
        get
        {
            return (ProviderConfigurationCollection)base["providers"];
        }
    }

    [ConfigurationProperty("defaultSerializer", IsRequired = false)]
    public string DefaultSerializer
    {
        get
        {
            return (string)this["defaultSerializer"];
        }
        set
        {
            this["defaultSerializer"] = value;
        }
    }

    [ConfigurationProperty("defaultConnection", IsRequired = true)]
    public ConnectionConfigurationElement DefaultConnection
    {
        get
        {
            return (ConnectionConfigurationElement)this["defaultConnection"];
        }
        set
        {
            this["defaultConnection"] = value;
        }
    }

    [ConfigurationProperty("connections")]
    [ConfigurationCollection(typeof(ConnectionConfigurationCollection),
        AddItemName = "add",
        ClearItemsName = "clear",
        RemoveItemName = "remove")]
    public ConnectionConfigurationCollection Connections
    {
        get
        {
            return (ConnectionConfigurationCollection)base["connections"];
        }
    }
}
#endif