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

    [ConfigurationProperty("defaultSerializer", IsRequired = false, IsKey = true)]
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

    [ConfigurationProperty("connections", IsDefaultCollection = true)]
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

    [ConfigurationProperty("providers", IsRequired = true)]
    [ConfigurationCollection(typeof(NameValueConfigurationCollection),
        AddItemName = "add",
        ClearItemsName = "clear",
        RemoveItemName = "remove")]
    public NameValueConfigurationCollection Providers
    {
        get
        {
            return (NameValueConfigurationCollection)this["providers"];
        }
    }
}
#endif