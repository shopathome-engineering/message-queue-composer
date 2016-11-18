using System.Configuration;

namespace ShopAtHome.TransactionProcessor.Configuration.ConfigFile
{
    internal class ConfigFileQueueConfigurationElement : BaseConfigurationElement, IQueueConfiguration
    {
        private const string NameKey = "name";
        private const string PollIntervalKey = "pollingInterval";
        private const string MessageCountThresholdKey = "queueMessageThreshold";
        private const string ReadIntervalThresholdKey = "readInterval";
        private const string TargetTimeToClearKey = "targetTimeToClear";

        [ConfigurationProperty(NameKey, IsRequired = true, DefaultValue = default(string))]
        public string QueueIdentifier
        {
            get { return GetConfigValue<string>(NameKey); }
            set { SetConfigValue(NameKey, value); }
        }

        [ConfigurationProperty(PollIntervalKey, IsRequired = true, DefaultValue = default(int))]
        public int PollingInterval
        {
            get { return GetConfigValue<int>(PollIntervalKey); }
            set { SetConfigValue(PollIntervalKey, value); }
        }

        [ConfigurationProperty(MessageCountThresholdKey, IsRequired = true, DefaultValue = default(int))]
        public int QueueReadMessageCountThreshold
        {
            get { return GetConfigValue<int>(MessageCountThresholdKey); }
            set { SetConfigValue(MessageCountThresholdKey, value); }
        }

        [ConfigurationProperty(ReadIntervalThresholdKey, IsRequired = true, DefaultValue = default(int))]
        public int QueueReadInterval
        {
            get { return GetConfigValue<int>(ReadIntervalThresholdKey); }
            set { SetConfigValue(ReadIntervalThresholdKey, value); }
        }

        [ConfigurationProperty(TargetTimeToClearKey, IsRequired = true, DefaultValue = default(int))]
        public int TargetTimeToClearQueue
        {
            get { return GetConfigValue<int>(TargetTimeToClearKey); }
            set { SetConfigValue(TargetTimeToClearKey, value); }
        }

        internal ConfigFileQueueConfigurationElement(string queueIdentifier, int pollingInternval, int readCountThreshold,
            int readInterval, int targetTimeToClearQueue)
        {
            QueueIdentifier = queueIdentifier;
            PollingInterval = pollingInternval;
            QueueReadMessageCountThreshold = readCountThreshold;
            QueueReadInterval = readInterval;
            TargetTimeToClearQueue = targetTimeToClearQueue;
        }

        internal ConfigFileQueueConfigurationElement()
        {
        }
    }
}
