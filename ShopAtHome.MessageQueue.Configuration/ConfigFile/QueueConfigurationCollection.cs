using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ShopAtHome.TransactionProcessor.Configuration.ConfigFile
{
    public class QueueConfigurationCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ConfigFileQueueConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfigFileQueueConfigurationElement) element).QueueIdentifier;
        }

        public IEnumerable<IQueueConfiguration> GetConfigurations()
        {
            return this.Cast<ConfigFileQueueConfigurationElement>();
        }
    }
}
