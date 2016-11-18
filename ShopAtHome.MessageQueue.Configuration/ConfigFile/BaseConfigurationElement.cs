using System.Configuration;

namespace ShopAtHome.TransactionProcessor.Configuration.ConfigFile
{
    internal abstract class BaseConfigurationElement : ConfigurationElement
    {
        protected T GetConfigValue<T>(string key)
        {
            return (T)this[key];
        }

        protected void SetConfigValue(string key, object value)
        {
            this[key] = value;
        }
    }
}
