using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ShopAtHome.TransactionProcessor.Configuration.ConfigFile
{
    public class ConfigFileProvider : IConfigurationProvider
    {
        private readonly IEnumerable<IQueueConfiguration> _configurations;

        public ConfigFileProvider()
        {
            var section = ConfigurationManager.GetSection(QueueConfigurationSection.SectionName) as QueueConfigurationSection;
            if (section == null)
            {
                throw new InvalidOperationException($"No configuration section with identifier {QueueConfigurationSection.SectionName} found in configuration file!");
            }
            _configurations = section.Queues.GetConfigurations();
        }


        public IQueueConfiguration GetQueueConfiguration(string queueIdentifier)
        {
            return _configurations.FirstOrDefault(x => x.QueueIdentifier == queueIdentifier);
        }

        public IWorkerConfiguration GetWorkerConfiguration(string queueIdentifier)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IQueueConfiguration> GetQueueConfigurations()
        {
            return _configurations;
        }

        public IQueueConfiguration GetListenerReportQueueConfiguration()
        {
            throw new NotImplementedException();
        }

        public IQueueConfiguration GetWorkerReportQueueConfiguration()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IListenerConfiguration> GetListenerConfigurations()
        {
            throw new NotImplementedException();
        }
    }
}
