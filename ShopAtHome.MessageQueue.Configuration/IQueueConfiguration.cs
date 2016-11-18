namespace ShopAtHome.MessageQueue.Configuration
{
    public interface IQueueConfiguration
    {
        /// <summary>
        /// The reference name of the queue in the message bus system
        /// </summary>
        string QueueIdentifier { get; }

        /// <summary>
        /// The period (in seconds) of polling the queue
        /// </summary>
        int PollingInterval { get; }

        /// <summary>
        /// The number of messages that have to be in the queue before data begins to be read from it
        /// </summary>
        int QueueReadMessageCountThreshold { get; }

        /// <summary>
        /// The period (in seconds) of reading from the queue, regardless of if the message number threshold has been reached
        /// </summary>
        int QueueReadInterval { get; }

        /// <summary>
        /// The target time (in seconds) in which the queue should be emptied
        /// </summary>
        int TargetTimeToClearQueue { get; }

        string ExchangeIdentifier { get; set; }

        string RoutingKey { get; set; }

        /// <summary>
        /// Namespace of the kind of data this queue deals in
        /// </summary>
        string DataType { get; set; }

        int SystemIdentifier { get; }
    }
}
