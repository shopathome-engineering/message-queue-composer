using System.Collections.Generic;
using ShopAtHome.MessageQueue.Consumers.Configuration;

namespace ShopAtHome.MessageQueue.Configuration
{
    public interface IConfigurationProvider
    {
        IQueueConfiguration GetQueueConfiguration(string queueIdentifier);
        IWorkerConfiguration GetWorkerConfiguration(string queueIdentifier);
        IEnumerable<IQueueConfiguration> GetQueueConfigurations(int systemIdentifier);
        IListenerConfiguration GetListenerConfiguration(string queueIdentifier);
        IExchangeConfiguration GetExchangeConfiguration(string exchangeIdentifier);
    }
}
