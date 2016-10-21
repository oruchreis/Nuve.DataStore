using System;
using System.Configuration;

namespace Nuve.DataStore.Configuration
{
    internal class ConnectionConfigurationCollection : ConfigurationElementCollection
    {
        public ConnectionConfigurationCollection()
        {
            Add((ConnectionConfigurationElement)CreateNewElement());
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ConnectionConfigurationElement();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((ConnectionConfigurationElement)element).Name;
        }

        public ConnectionConfigurationElement this[int index]
        {
            get
            {
                return (ConnectionConfigurationElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public new ConnectionConfigurationElement this[string Name]
        {
            get
            {
                return (ConnectionConfigurationElement)BaseGet(Name);
            }
        }

        public int IndexOf(ConnectionConfigurationElement url)
        {
            return BaseIndexOf(url);
        }

        public void Add(ConnectionConfigurationElement element)
        {
            BaseAdd(element);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);
        }

        public void Remove(ConnectionConfigurationElement url)
        {
            if (BaseIndexOf(url) >= 0)
                BaseRemove(url.Name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void Clear()
        {
            BaseClear();
        }
    }
}