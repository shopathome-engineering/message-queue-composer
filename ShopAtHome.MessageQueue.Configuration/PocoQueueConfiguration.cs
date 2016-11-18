using System.ComponentModel.DataAnnotations.Schema;

namespace ShopAtHome.MessageQueue.Configuration
{
    /// <summary>
    /// POCO implementation of the IQueueConfiguration interface. Setters are only on the implementation (used for hydration/deserialization, etc.)
    /// </summary>
    [Table("QueueConfiguration")]
    public class PocoQueueConfiguration : IQueueConfiguration
    {
        public string QueueIdentifier { get; set; }
        public int PollingInterval { get; set; }
        public int QueueReadMessageCountThreshold { get; set; }
        public int QueueReadInterval { get; set; }
        public int TargetTimeToClearQueue { get; set; }
        public string ExchangeIdentifier { get; set; }
        public string RoutingKey { get; set; }
        public string DataType { get; set; }
        public int SystemIdentifier { get; set; }
    }
}
