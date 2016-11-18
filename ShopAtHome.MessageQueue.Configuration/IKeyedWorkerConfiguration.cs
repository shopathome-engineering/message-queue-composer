using System;
using ShopAtHome.MessageQueue.Consumers.Configuration;

namespace ShopAtHome.MessageQueue.Configuration
{
    public interface IKeyedWorkerConfiguration : IWorkerConfiguration
    {
        object Key { get; set; }

        Type KeyType { get; set; }
    }
}
