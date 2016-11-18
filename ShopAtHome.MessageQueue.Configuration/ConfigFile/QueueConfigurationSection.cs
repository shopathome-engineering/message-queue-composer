using System.Configuration;

namespace ShopAtHome.TransactionProcessor.Configuration.ConfigFile
{
    public class QueueConfigurationSection : ConfigurationSection
    {
        internal static string SectionName = "QueueConfiguration";
        private const string ConfigCollectionName = "queues";

        static QueueConfigurationSection()
        {
            var section = ConfigurationManager.GetSection(SectionName);
            Config = (QueueConfigurationSection)section;
        }

        [ConfigurationProperty(ConfigCollectionName, IsDefaultCollection = false, IsRequired = true)]
        [ConfigurationCollection(typeof(QueueConfigurationCollection), AddItemName = "queue")]
        public QueueConfigurationCollection Queues => (QueueConfigurationCollection)base[ConfigCollectionName];

        public static QueueConfigurationSection Config { get; }
    }
}
