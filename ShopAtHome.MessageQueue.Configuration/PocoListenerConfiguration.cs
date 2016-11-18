using System;
using System.ComponentModel.DataAnnotations.Schema;
using ShopAtHome.MessageQueue.Consumers.Configuration;

namespace ShopAtHome.MessageQueue.Configuration
{
    /// <summary>
    /// POCO implementation of the IListenerConfiguration interface. Setters are only on the implementation (used for hydration/deserialization, etc.)
    /// </summary>
    [Table("ListenerConfiguration")]
    public class PocoListenerConfiguration : IListenerConfiguration
    {
        public Type ActorType { get; set; }
        public string SourceQueue { get; set; }
        public string ReportQueue { get; set; }
        public int PollingInterval { get; set; }
        public int QueueReadInterval { get; set; }
        public int QueueReadMessageCountThreshold { get; set; }
    }
}
