using System;
using Autofac;
using ShopAtHome.MessageQueue.Consumers;
using ShopAtHome.MessageQueue.Consumers.Configuration;

namespace ShopAtHome.MessageQueue.Composer
{
    /// <summary>
    /// A class used to instantiate actors and set their initial configuration
    /// </summary>
    public class ActorFactory
    {
        /// <summary>
        /// Creates a T and configures it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dependencyResolver"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static T Create<T>(IContainer dependencyResolver, IActorConfiguration configuration) where T : IActor
        {
            return (T)Create(dependencyResolver, configuration);
        }

        /// <summary>
        /// Creates an actor according to the actor configuration's specified type and configures it
        /// </summary>
        /// <param name="dependencyResolver"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IActor Create(IContainer dependencyResolver, IActorConfiguration configuration)
        {
            return ((IActor)dependencyResolver.Resolve(configuration.ActorType)).Configure(configuration);
        }

        /// <summary>
        /// Creates an actor with a key in the constructor
        /// </summary>
        /// <param name="dependencyResolver"></param>
        /// <param name="configuration"></param>
        /// <param name="key"></param>
        /// <param name="keyType"></param>
        /// <returns></returns>
        public static IActor CreateWithKeyDependency(IContainer dependencyResolver, IActorConfiguration configuration, object key, Type keyType)
        {
            // Strange behavior from JSON.NET - apparently if you serialize an integer but with object metadata, it gets de-serialized
            // as a 64-bit integer! So we need to change it manually back to the declared type
            var realKey = Convert.ChangeType(key, keyType);
            return ((IActor)dependencyResolver.Resolve(configuration.ActorType, new TypedParameter(keyType, realKey))).Configure(configuration);
        }
    }
}
