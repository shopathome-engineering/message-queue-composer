namespace ShopAtHome.MessageQueue.Configuration
{
    public interface IExchangeConfiguration
    {
        string ExchangeIdentifier { get; }

        string RoutingKeyFormat { get; }

        string Type { get; }

        string AlternateExchangeIdentifier { get; }
    }
}
