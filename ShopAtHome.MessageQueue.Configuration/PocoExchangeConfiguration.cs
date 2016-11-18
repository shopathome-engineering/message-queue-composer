using System.ComponentModel.DataAnnotations.Schema;

namespace ShopAtHome.MessageQueue.Configuration
{
    /// <summary>
    /// POCO implementation of the IExchangeConfiguration interface. Setters are only on the implementation (used for hydration/deserialization, etc.)
    /// </summary>
    [Table("ExchangeConfiguration")]
    public class PocoExchangeConfiguration : IExchangeConfiguration
    {
        public string ExchangeIdentifier { get; set; }
        public string RoutingKeyFormat { get; set; }
        public string Type { get; set; }
        public string AlternateExchangeIdentifier { get; set; }
    }
}
