using System;

namespace ShopAtHome.MessageQueue.Composer
{
    public class ComposerActionRequest
    {
        public Type ListenerType { get; set; }
        public string SourceQueueIdentifier { get; set; }
        public Type WorkerType { get; set; }
        public string WorkerNextQueueIdentifier { get; set; }
        public object Key { get; set; }

        public Type KeyType { get; set; }
    }
}
