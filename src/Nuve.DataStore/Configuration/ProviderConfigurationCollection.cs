#if NET48
using System.Configuration;

namespace Nuve.DataStore.Configuration
{
    internal class ProviderConfigurationCollection : ConfigurationElementCollection
    {
        public ProviderConfigurationCollection()
        {
            Add((ProviderConfigurationElement)CreateNewElement());
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
            return new ProviderConfigurationElement();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((ProviderConfigurationElement)element).Name;
        }

        public ProviderConfigurationElement this[int index]
        {
            get
            {
                return (ProviderConfigurationElement)BaseGet(index);
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

        public new ProviderConfigurationElement this[string Name]
        {
            get
            {
                return (ProviderConfigurationElement)BaseGet(Name);
            }
        }

        public int IndexOf(ProviderConfigurationElement url)
        {
            return BaseIndexOf(url);
        }

        public void Add(ProviderConfigurationElement element)
        {
            BaseAdd(element);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);
        }

        public void Remove(ProviderConfigurationElement url)
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
#endif