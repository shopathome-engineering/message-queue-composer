using System;
using System.ComponentModel.DataAnnotations.Schema;
using ShopAtHome.MessageQueue.Consumers.Configuration;

namespace ShopAtHome.MessageQueue.Configuration
{
    /// <summary>
    /// POCO implementation of the IWorkerConfiguration interface. Setters are only on the implementation (used for hydration/deserialization, etc.)
    /// </summary>
    [Table("WorkerConfiguration")]
    public class PocoWorkerConfiguration : IWorkerConfiguration
    {
        public Type ActorType { get; set; }
        public string SourceQueue { get; set; }
        public string NextQueue { get; set; }
        public string ReportQueue { get; set; }
    }
}
