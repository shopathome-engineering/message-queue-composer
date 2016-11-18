using System;
using System.Collections.Generic;
using Autofac;
using ShopAtHome.MessageQueue.Consumers;
using ShopAtHome.MessageQueue.Consumers.Configuration;
using ShopAtHome.MessageQueue.Consumers.Messages;

namespace ShopAtHome.MessageQueue.Composer
{
    public interface IActorManager
    {
        Action<Exception> OnErrorBehavior { get; set; }

        void StartActor(string actorGroupingIdentifier, BaseWorker actor);

        void StartActor(Listener actor);

        void Deactivate(string actorGroupingIdentifier, BaseWorker actor);

        /// <summary>
        /// Deactivates the worker associated with the provided report
        /// </summary>
        /// <param name="report"></param>
        void Deactivate(WorkerReport report);

        void Deactivate(Listener actor);

        /// <summary>
        /// Deactivates all actors
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Locates workers which have been deactivated and removes them
        /// </summary>
        void PruneInactiveWorkers();

        /// <summary>
        /// Locates inactive listeners and replaces them with new ones
        /// </summary>
        /// <param name="listenerConfigurations"></param>
        /// <param name="dependencyResolver"></param>
        void ReplaceInactiveListeners(List<IListenerConfiguration> listenerConfigurations, IContainer dependencyResolver);

        int GetWorkerCount(string queueIdentifier);

        void CreateAndStartWorker(IWorkerConfiguration configuration, IContainer dependencyResolver);
        void CreateAndStartKeyedSoloWorker(IWorkerConfiguration configuration, IContainer dependencyResolver, object key, Type keyType);
        void CreateAndStartListener(IListenerConfiguration listenerConfiguration, IContainer dependencyResolver);
        void CreateAndStartKeyedSoloListener(IListenerConfiguration listenerConfiguration, IContainer dependencyResolver, object key, Type keyType);
    }
}
