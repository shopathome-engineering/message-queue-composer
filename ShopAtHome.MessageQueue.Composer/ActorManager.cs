using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using ShopAtHome.MessageQueue.Configuration;
using ShopAtHome.MessageQueue.Consumers;
using ShopAtHome.MessageQueue.Consumers.Configuration;
using ShopAtHome.MessageQueue.Consumers.Messages;

namespace ShopAtHome.MessageQueue.Composer
{
    public class ActorManager : IActorManager
    {
        private static readonly ConcurrentDictionary<string, ConcurrentBag<BaseWorker>> ActiveWorkers = new ConcurrentDictionary<string, ConcurrentBag<BaseWorker>>();
        private static readonly ConcurrentDictionary<string, Listener> ActiveListeners = new ConcurrentDictionary<string, Listener>();

        public Action<Exception> OnErrorBehavior { get; set; }

        public void StartActor(string actorGroupingIdentifier, BaseWorker actor)
        {
            var key = DebugModeStringHandler.UnmakeDebugIdentifierValue(actorGroupingIdentifier);
            Start(actor);
            if (!ActiveWorkers.ContainsKey(key))
            {
                ActiveWorkers.GetOrAdd(key, new ConcurrentBag<BaseWorker>());
            }
            ActiveWorkers[key].Add(actor);
        }

        public void StartActor(Listener actor)
        {
            Start(actor);
            var key = DebugModeStringHandler.UnmakeDebugIdentifierValue(actor.QueueBeingMonitored);
            if (ActiveListeners.ContainsKey(key))
            {
                ActiveListeners[key] = actor;
            }
            else
            {
                ActiveListeners.TryAdd(key, actor);
            }
        }

        public void Deactivate(string actorGroupingIdentifier, BaseWorker actor)
        {
            var key = DebugModeStringHandler.UnmakeDebugIdentifierValue(actorGroupingIdentifier);
            actor.TurnOff();
            if (!ActiveWorkers.ContainsKey(key))
            {
                return;
            }
            ActiveWorkers[key].TryTake(out actor);
        }

        public void Deactivate(WorkerReport report)
        {
            var workedQueue = DebugModeStringHandler.UnmakeDebugIdentifierValue(report.SourceQueue);
            if (!ActiveWorkers.ContainsKey(workedQueue))
            {
                return;
            }
            var worker = ActiveWorkers[workedQueue].FirstOrDefault(x => x.Id == report.WorkerId);
            if (worker == null)
            {
                // Someone else got it before we did
                return;
            }
            Deactivate(report.SourceQueue, worker);
        }

        public void Deactivate(Listener actor)
        {
            actor.TurnOff();
            ActiveListeners.TryRemove(DebugModeStringHandler.UnmakeDebugIdentifierValue(actor.QueueBeingMonitored), out actor);
        }

        public void DeactiveListener(string sourceQueueIdentifier)
        {
            if (!ActiveListeners.ContainsKey(sourceQueueIdentifier))
            {
                return;
            }
            Deactivate(ActiveListeners[sourceQueueIdentifier]);
        }

        public void Deactivate()
        {
            foreach (var workedQueue in ActiveWorkers)
            {
                foreach (var worker in workedQueue.Value.Select(x => x))
                {
                    Deactivate(workedQueue.Key, worker);
                }
            }
            foreach (var listener in ActiveListeners.Values)
            {
                Deactivate(listener);
            }
        }

        public void PruneInactiveWorkers()
        {
            var deadWorkers = new Dictionary<string, IEnumerable<BaseWorker>>();
            foreach (var workerSet in ActiveWorkers)
            {
                deadWorkers[workerSet.Key] = workerSet.Value.Where(worker => !worker.IsRunning);
            }
            foreach (var deadWorkerSet in deadWorkers)
            {
                foreach (var deadWorker in deadWorkerSet.Value)
                {
                    Deactivate(deadWorkerSet.Key, deadWorker);
                }
            }
        }

        public void ReplaceInactiveListeners(List<IListenerConfiguration> listenerConfigurations, IContainer dependencyResolver)
        {
            var listenersToReplace = ActiveListeners.Values.Where(listener => !listener.IsRunning).ToList();
            foreach (var listener in listenersToReplace)
            {
                var configuration = listenerConfigurations.FirstOrDefault(x => x.SourceQueue == listener.QueueBeingMonitored);
                if (configuration == null)
                {
                    Listener unconfiguredListener;
                    ActiveListeners.TryRemove(listener.QueueBeingMonitored, out unconfiguredListener);
                    continue;
                }

                var newListener = ActorFactory.Create<Listener>(dependencyResolver, configuration).ConfigureListener(configuration);
                StartActor(newListener);
            }
        }

        public int GetWorkerCount(string queueIdentifier)
        {
            var key = DebugModeStringHandler.UnmakeDebugIdentifierValue(queueIdentifier);
            return ActiveWorkers.ContainsKey(key) ? ActiveWorkers[key].Count : 0;
        }

        public void CreateAndStartWorker(IWorkerConfiguration configuration, IContainer dependencyResolver)
        {
            var worker = ActorFactory.Create<BaseWorker>(dependencyResolver, configuration);
            worker.ConfigureWorker(configuration);
            StartActor(configuration.SourceQueue, worker);
        }

        public void CreateAndStartKeyedSoloWorker(IWorkerConfiguration configuration, IContainer dependencyResolver, object key, Type keyType)
        {
            var worker = (BaseWorker) ActorFactory.CreateWithKeyDependency(dependencyResolver, configuration, key, keyType);
            worker.ConfigureWorker(configuration);
            StartActor(key.ToString(), worker);
        }

        public void CreateAndStartListener(IListenerConfiguration listenerConfiguration, IContainer dependencyResolver)
        {
            var listenWorker = ActorFactory.Create<Listener>(dependencyResolver, listenerConfiguration).ConfigureListener(listenerConfiguration);
            StartActor(listenWorker);
        }

        public void CreateAndStartKeyedSoloListener(IListenerConfiguration listenerConfiguration, IContainer dependencyResolver, object key, Type keyType)
        {
            var listenWorker = ((Listener)ActorFactory.CreateWithKeyDependency(dependencyResolver, listenerConfiguration, key, keyType)).ConfigureListener(listenerConfiguration);
            StartActor(listenWorker);
        }

        private void Start(IActor actor)
        {
            Task.Run(() =>
            {
                try
                {
                    actor.Work();
                }
                catch (Exception ex)
                {
                    OnErrorBehavior?.Invoke(ex);
                    throw;
                }
            });
        }
    }
}
