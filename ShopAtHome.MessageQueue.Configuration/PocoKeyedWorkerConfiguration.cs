using System;

namespace ShopAtHome.MessageQueue.Configuration
{
    public class PocoKeyedWorkerConfiguration : PocoWorkerConfiguration, IKeyedWorkerConfiguration
    {
        public object Key { get; set; }
        public Type KeyType { get; set; }
    }
}
