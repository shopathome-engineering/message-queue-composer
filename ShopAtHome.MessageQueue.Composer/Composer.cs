using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ShopAtHome.MessageQueue.Configuration;
using ShopAtHome.MessageQueue.Consumers;
using ShopAtHome.MessageQueue.Consumers.Configuration;
using ShopAtHome.MessageQueue.Consumers.Messages;
using ShopAtHome.MessageQueue.Statistics;
using IContainer = Autofac.IContainer;

namespace ShopAtHome.MessageQueue.Composer
{
    /// <summary>
    /// The composition root of applications that make use of the message queue architecture
    /// </summary>
    /// <remarks>This "DesignerCategory" attribute is, irritatingly, the only way to get Visual Studio to stop opening this file in Designer view</remarks>
    [DesignerCategory("Code")]
    public abstract class Composer : ServiceBase
    {
        private readonly IConnectionProvider _connectionFactory;
        private readonly IProcessorStatisticsProvider _statisticsProvider;
        private readonly IActorManager _actorManager;
        private readonly IContainer _dependencyResolver;
        private static readonly ManualResetEventSlim ShutdownEvent = new ManualResetEventSlim(false);
        private static readonly ManualResetEventSlim ListenerSubscriptionEnded = new ManualResetEventSlim();
        private static readonly ManualResetEventSlim WorkerSubscriptionEnded = new ManualResetEventSlim();
        private static readonly ManualResetEventSlim ActionSubscriptionEnded = new ManualResetEventSlim();
        private List<IQueueConfiguration> _queueConfigurations;
        private List<IWorkerConfiguration> _workerConfigurations;
        private Dictionary<string, IKeyedWorkerConfiguration> _dynamicallyKeyedWorkerConfigurations;
        private List<IListenerConfiguration> _listenerConfigurations;
        private readonly int _systemId;
        private readonly Guid _instanceId;
        private readonly IEnumerable<IExternalDependency> _externalDependencies;
        private List<List<string>> _orderedWorkflowQueueIdentifiers;

        /// <summary>
        /// Used in Catch blocks - typically you would put some kind of logging action in here
        /// </summary>
        protected abstract Action<Exception> OnErrorAction { get; }

        /// <summary>
        /// Initializes the composer with its required dependencies from the provided container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="systemId">The integer identifier of this system</param>
        protected Composer(IContainer container, int systemId)
        {
            _systemId = systemId;
            _instanceId = Guid.NewGuid();
            try
            {
                _dependencyResolver = container;
                _actorManager = container.Resolve<IActorManager>();
                _statisticsProvider = container.Resolve<IProcessorStatisticsProvider>();
                _connectionFactory = container.Resolve<IConnectionProvider>();
                // There may not be any external dependencies for this system
                _externalDependencies = container.IsRegistered<IEnumerable<IExternalDependency>>() ? container.Resolve<IEnumerable<IExternalDependency>>() : new List<IExternalDependency>();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(GetType().Name, "Fatal exception encountered during start-up: \n" + ex, EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Called when the WinServiceHost starts this application
        /// </summary>
        /// <param name="args"></param>
        public void Start(string[] args)
        {
            _actorManager.OnErrorBehavior = OnErrorAction;
            OnStart(args);
        }

        /// <summary>
        /// Called at the stop of the application
        /// </summary>
        protected override void OnStop()
        {
            ShutdownEvent.Set();
            WorkerSubscriptionEnded.Set();
            ListenerSubscriptionEnded.Set();
            ActionSubscriptionEnded.Set();
            _actorManager.Deactivate();
            CleanupSystemQueues();
            base.OnStop();
        }

        /// <summary>
        /// Called at the start of the service
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            Configure(_dependencyResolver.Resolve<IConfigurationProvider>());
            foreach (var listenerConfiguration in _listenerConfigurations)
            {
                _actorManager.CreateAndStartListener(listenerConfiguration, _dependencyResolver);
            }

            SubscribeToListenerReports();
            SubscribeToWorkerReports();
            SubscribeToActionRequests();

            Task.Run(() =>
            {
                DoWithTryCatch(ListenerHealthCheck);
            });

            Task.Run(() =>
            {
                DoWithTryCatch(WorkerHealthCheck);
            });

            //TODO: "composer action" queue, requests for data and instructions on how to behave

            base.OnStart(args);
        }

        private void SubscribeToActionRequests()
        {
            ActionSubscriptionEnded.Reset();
            Task.Run(() =>
            {
                DoWithTryCatch(() =>
                {
                    using (var actionRequestConnection = _connectionFactory.ConnectToQueue<ComposerActionRequest>(GetComposerActionQueueName()))
                    {
                        actionRequestConnection.Subscribe(HandleActionRequest, ActionSubscriptionEnded.Set);
                        ActionSubscriptionEnded.Wait();
                    }
                }, ActionSubscriptionEnded.Set);
            });
        }

        private void SubscribeToWorkerReports()
        {
            WorkerSubscriptionEnded.Reset();
            Task.Run(() =>
            {
                DoWithTryCatch(() =>
                {
                    using (var workerReportConnection = _connectionFactory.ConnectToQueue<WorkerReport>(GetWorkerReportQueueName()))
                    {
                        workerReportConnection.Subscribe(EvaluateWorkerReport, WorkerSubscriptionEnded.Set);
                        WorkerSubscriptionEnded.Wait();
                    }
                }, WorkerSubscriptionEnded.Set);
            });
        }

        private void SubscribeToListenerReports()
        {
            ListenerSubscriptionEnded.Reset();
            Task.Run(() =>
            {
                DoWithTryCatch(() =>
                {
                    using (var listenerReportConnection = _connectionFactory.ConnectToQueue<ListenerReport>(GetListenerReportQueueName()))
                    {
                        listenerReportConnection.Subscribe(EvaluateListenerReport, ListenerSubscriptionEnded.Set);
                        ListenerSubscriptionEnded.Wait();
                    }
                }, ListenerSubscriptionEnded.Set);
            });
        }

        private void DoWithTryCatch(Action a)
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                OnErrorAction(ex);
            }
        }

        private void DoWithTryCatch(Action a, Action onError)
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                OnErrorAction(ex);
                onError();
            }
        }

        /// <summary>
        /// Finds workers that are not running and removes them from the collection of active workers, so that new ones can be made to replace them
        /// </summary>
        private void WorkerHealthCheck()
        {
            while (true)
            {
                try
                {
                    if (ShutdownEvent.IsSet)
                    {
                        return;
                    }
                    _actorManager.PruneInactiveWorkers();
                    if (WorkerSubscriptionEnded.IsSet)
                    {
                        // If our composer's subscription to worker reports has fallen over, stand it back up
                        SubscribeToWorkerReports();
                    }
                    // Dumb 30 second long sleep
                    Thread.Sleep(30*1000);
                }
                catch (Exception ex)
                {
                    OnErrorAction(ex);
                }
            }
        }

        /// <summary>
        /// Finds listeners that are not running and replaces them with new, running ones
        /// </summary>
        private void ListenerHealthCheck()
        {
            while (true)
            {
                try
                {
                    if (ShutdownEvent.IsSet)
                    {
                        return;
                    }
                    _actorManager.ReplaceInactiveListeners(_listenerConfigurations, _dependencyResolver);
                    if (ListenerSubscriptionEnded.IsSet)
                    {
                        // If our composer's subscription to listener reports has fallen over, stand it back up
                        SubscribeToListenerReports();
                    }
                    // Dumb minute long sleep
                    Thread.Sleep(60*1000);
                }
                catch (Exception ex)
                {
                    OnErrorAction(ex);
                }
            }
        }

        private static string GetConfigValue(string key)
        {
            var baseValue = ConfigurationManager.AppSettings.Get(key);
            if (baseValue == null) { throw new ConfigurationErrorsException($"Expected to find app setting with key {key}, but did not"); }
            return DebugModeStringHandler.MakeDebugIdentifierValue(baseValue);
        }

        internal string GetWorkerReportQueueName()
        {
            return $"{Environment.MachineName}_WorkerReport_{_instanceId}";
        }

        internal string GetListenerReportQueueName()
        {
            return $"{Environment.MachineName}_ListenerReport_{_instanceId}";
        }

        public static string GetComposerActionQueueName()
        {
            // TODO: Will need to make these queues unique - per - instance if we ever have more than one Composer app floating around
            // We could do this any number of ways, I'm not bothering at the moment just for the sake of time
            // Could use a static instance dictionary, could make this abstract, etc.
            return $"{Environment.MachineName}_ActionRequest";
        }

        private void CleanupSystemQueues()
        {
            _connectionFactory.DeleteQueue(GetWorkerReportQueueName());
            _connectionFactory.DeleteQueue(GetListenerReportQueueName());
            _connectionFactory.DeleteQueue(GetComposerActionQueueName());
        }

        private void Configure(IConfigurationProvider configurationProvider)
        {
            try
            {
                _queueConfigurations = configurationProvider.GetQueueConfigurations(_systemId).ToList();
                // Not all queues may have workers or listeners defined, so strip out those that do not
                // (an example would be system queues, e.g the report queues)
                _workerConfigurations = _queueConfigurations.Select(x => configurationProvider.GetWorkerConfiguration(x.QueueIdentifier)).Where(x => x != null).ToList();
                _listenerConfigurations = _queueConfigurations.Select(x => GetListenerConfiguration(x, configurationProvider)).Where(x => x != null).ToList();
                _dynamicallyKeyedWorkerConfigurations = new Dictionary<string, IKeyedWorkerConfiguration>();

                // Need to create exchanges before we create queues
                EnsureExchanges(configurationProvider);
                EnsureWorkflowQueues();
                CreateSystemQueues();
                _orderedWorkflowQueueIdentifiers = Utilities.CreateOrderedWorkflows(_workerConfigurations);
            }
            catch (Exception ex)
            {
                OnErrorAction(ex);
                throw;
            }
        }

        internal IListenerConfiguration GetListenerConfiguration(IQueueConfiguration queueToMonitor, IConfigurationProvider configurationProvider)
        {
            if (queueToMonitor == null) { return null; }
            var result = configurationProvider.GetListenerConfiguration(DebugModeStringHandler.UnmakeDebugIdentifierValue(queueToMonitor.QueueIdentifier));
            if (result == null) { return null; }
            result.ReportQueue = GetListenerReportQueueName();
            result.PollingInterval = queueToMonitor.PollingInterval;
            result.QueueReadInterval = queueToMonitor.QueueReadInterval;
            result.QueueReadMessageCountThreshold = queueToMonitor.QueueReadMessageCountThreshold;
            return result;
        }

        /// <summary>
        /// Creates the queues that this system uses to communicate with itself - report queues for its listeners and workers
        /// </summary>
        private void CreateSystemQueues()
        {
            foreach (var queueInfo in new[]
            {
                new PocoQueueConfiguration
                {
                    QueueIdentifier = GetListenerReportQueueName()
                },
                new PocoQueueConfiguration
                {
                    QueueIdentifier = GetWorkerReportQueueName()
                },
                new PocoQueueConfiguration
                {
                    QueueIdentifier = GetComposerActionQueueName()
                }
            }.Select(GetQueueCreationInfo))
            {
                _connectionFactory.CreateQueue(queueInfo);
            }
        }

        /// <summary>
        /// Ensures that the queues belonging to this system's workflow exist with the specified configuration
        /// </summary>
        private void EnsureWorkflowQueues()
        {
            foreach (var queueInfo in _queueConfigurations.Select(GetQueueCreationInfo))
            {
                _connectionFactory.CreateQueue(queueInfo);
            }
        }

        /// <summary>
        /// Ensures that the exchanges belonging to this system's workflow exist with the specified configuration
        /// </summary>
        private void EnsureExchanges(IConfigurationProvider configurationProvider)
        {
            var exchangeConfigurations = _queueConfigurations.Where(x => !string.IsNullOrEmpty(x.ExchangeIdentifier)).Select(x => configurationProvider.GetExchangeConfiguration(x.ExchangeIdentifier));
            foreach (var exchange in exchangeConfigurations)
            {
                _connectionFactory.CreateExchange(exchange.ExchangeIdentifier, exchange.Type, exchange.AlternateExchangeIdentifier);
            }
        }

        private QueueCreationInfo GetQueueCreationInfo(IQueueConfiguration queue)
        {
            try
            {
                var queueInfo = new QueueCreationInfo(queue.QueueIdentifier, true);
                if (!string.IsNullOrEmpty(queue.ExchangeIdentifier))
                {
                    queueInfo.BindingInfo = new ExchangeBindingInfo
                    {
                        ExchangeIdentifier = queue.ExchangeIdentifier,
                        RoutingKey = queue.RoutingKey ?? string.Empty // Rabbit MQ barfs on null strings
                    };
                }

                return queueInfo;
            }
            catch (Exception ex)
            {
                OnErrorAction(ex);
                throw;
            }
        }

        protected virtual void HandleActionRequest(Message<ComposerActionRequest> requestMessage)
        {
            try
            {
                if (requestMessage.Data.Count > 1)
                {
                    // this method assumes that only one report is contained per message, so die here - something's gotten messed up
                    throw new InvalidOperationException(
                        "More than the expected message count was received. This indicates a bug in the program.");
                }

                var request = requestMessage.Data.First();
                if (_dynamicallyKeyedWorkerConfigurations.ContainsKey(request.SourceQueueIdentifier))
                {
                    // Already got this handled, can just exit out
                    return;
                }
                // Right now the only kind of request we can make is that a new solo listener is attached to a queue
                // We can extend this functionality in the future if necessary
                var listenerConfiguration = new PocoListenerConfiguration
                {
                    ActorType = request.ListenerType,
                    PollingInterval = 10,
                    QueueReadInterval = 10,
                    QueueReadMessageCountThreshold = 1,
                    ReportQueue = GetListenerReportQueueName(),
                    SourceQueue = request.SourceQueueIdentifier
                };
                var workerConfiguration = new PocoKeyedWorkerConfiguration
                {
                    ActorType = request.WorkerType,
                    NextQueue = request.WorkerNextQueueIdentifier,
                    ReportQueue = GetWorkerReportQueueName(),
                    SourceQueue = request.SourceQueueIdentifier,
                    KeyType = request.KeyType,
                    Key = request.Key
                };
                _dynamicallyKeyedWorkerConfigurations.Add(request.SourceQueueIdentifier, workerConfiguration);
                _actorManager.CreateAndStartKeyedSoloListener(listenerConfiguration, _dependencyResolver, request.Key, request.KeyType);
                _listenerConfigurations.Add(listenerConfiguration);
            }
            catch (Exception ex)
            {
                OnErrorAction(ex);
            }
        }

        /// <summary>
        /// The action that is taken for each listener report
        /// </summary>
        /// <param name="report"></param>
        protected virtual void EvaluateListenerReport(Message<ListenerReport> report)
        {
            try
            {
                if (report.Data.Count > 1)
                {
                    // this method assumes that only one report is contained per message, so die here - something's gotten messed up
                    throw new InvalidOperationException(
                        "More than the expected message count was received. This indicates a bug in the program.");
                }

                var reportData = report.Data.First();
                var queueConfiguration = _queueConfigurations.FirstOrDefault(x => x.QueueIdentifier == reportData.Queue);
                if (queueConfiguration == null && _dynamicallyKeyedWorkerConfigurations.ContainsKey(reportData.Queue))
                {
                    // A request-driven queue, which means we need to...
                    var dynamicWorker = _dynamicallyKeyedWorkerConfigurations[reportData.Queue];
                    _actorManager.CreateAndStartKeyedSoloWorker(dynamicWorker, _dependencyResolver, dynamicWorker.Key, dynamicWorker.KeyType);
                    return;
                }

                var numActiveWorkers = _actorManager.GetWorkerCount(reportData.Queue);
                var numWorkersRequired = Utilities.GetNumWorkersRequiredToClearQueue(queueConfiguration, _statisticsProvider, reportData.MessageCount);
                if (numActiveWorkers >= numWorkersRequired)
                {
                    // We gucci
                    return;
                }

                var externalSystemStates = new List<DependencyInfo>();
                Parallel.ForEach(_externalDependencies, dependency =>
                {
                    externalSystemStates.Add(dependency.GetCurrentState());
                });

                var canScale = externalSystemStates.All(x => x.CanScaleUp);
                var mostLimitedSystem = externalSystemStates.OrderBy(x => x.PercentAvailableResources).FirstOrDefault();

                if (!canScale && numActiveWorkers > 0)
                {
                    // One of our dependencies is resource-capped, so we're going to wait until things settle down
                    return;
                }
                if (!canScale)
                {
                    // If a dependency is reporting that it can't take any additional load, but we don't have *any* workers, make one
                    // We don't want to STOP processing, just slow down
                    numWorkersRequired = 1;
                }

                var numNewWorkers = numWorkersRequired - numActiveWorkers;
                var affectedWorkflow = _orderedWorkflowQueueIdentifiers.FirstOrDefault(x => x.Contains(reportData.Queue)) ?? new List<string>();
                numNewWorkers = Utilities.GetNumWorkersToScaleWith(numNewWorkers, reportData.Queue, mostLimitedSystem, affectedWorkflow);

                var workerConfiguration = GetWorkerConfiguration(reportData.Queue);
                for (var i = 0; i < numNewWorkers; i++)
                {
                    if (numActiveWorkers >= _actorManager.MaxNumWorkersPerGroup)
                    {
                        break;
                    }
                    _actorManager.CreateAndStartWorker(workerConfiguration, _dependencyResolver);
                    numActiveWorkers++;
                }
            }
            catch (Exception ex)
            {
                OnErrorAction(ex);
            }
        }

        private IWorkerConfiguration GetWorkerConfiguration(string sourceQueue)
        {
            var result = _workerConfigurations.FirstOrDefault(x => x.SourceQueue == sourceQueue);
            if (result != null)
            {
                result.ReportQueue = GetWorkerReportQueueName();
            }
            return result;
        }

        /// <summary>
        /// The action that is taken for each worker report
        /// </summary>
        /// <param name="report"></param>
        protected virtual void EvaluateWorkerReport(Message<WorkerReport> report)
        {
            try
            {
                var reportData = ValidateWorkerReport(report);
                // TODO: May want to replace the switch with strategy. Need to see which is better as we add more behavior
                switch (reportData.Status)
                {
                    case WorkerReportStatus.TaskComplete:
                        // Good work, let's track the success and the elapsed time so we can make better decisions going forward
                        _statisticsProvider.RecordQueueMessageCompletionTime(reportData.SourceQueue, reportData.ElapsedTime);
                        break;
                    case WorkerReportStatus.NoWork:
                        // Uh oh, this worker doesn't have anything to do. Better turn it off
                        _actorManager.Deactivate(reportData);
                        if (_dynamicallyKeyedWorkerConfigurations.ContainsKey(reportData.SourceQueue))
                        {
                            _dynamicallyKeyedWorkerConfigurations.Remove(reportData.SourceQueue);
                            _actorManager.DeactiveListener(reportData.SourceQueue);
                        }
                        break;
                    case WorkerReportStatus.FatalError:
                        // For now, just turn the dead worker off
                        _actorManager.Deactivate(reportData);
                        break;
                    case WorkerReportStatus.ExileMessage:
                        // A message has been causing our workers to die regularly! They've already removed it from their work queue,
                        // But we need to be sure to put it somewhere where we can review it and identify the issue.
                        PlaceMessageIntoHoldingQueue(reportData.SourceQueue, reportData.Exception, reportData.Message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            $"{reportData.Status} does not map to enum {typeof(WorkerReportStatus).FullName}, or its case is not handled by this switch statement");
                }
            }
            catch (Exception ex)
            {
                OnErrorAction(ex);
            }
        }

        private static WorkerReport ValidateWorkerReport(Message<WorkerReport> report)
        {
            if (report.Data.Count != 1)
            {
                // this method assumes that only one report is contained per message, so die here - something's gotten messed up
                throw new InvalidOperationException(
                    $"An unexpected message count was received. This indicates a bug in the program. The message count was {report.Data.Count}");
            }

            var reportData = report.Data.First();
            if (string.IsNullOrEmpty(reportData.SourceQueue))
            {
                // Yikes, one of our workers isn't reporting the information we need to evaluate its report.
                // This is unrecoverable - we *have* to know which queue the worker is working on in order to proceed
                throw new InvalidOperationException(
                    $"A worker identified by {reportData.WorkerId} failed to report its source queue when sending a worker report with status {reportData.Status}." +
                    "We cannot proceed with evaluating its report. This indicates a serious flaw in this worker.");
            }
            if (reportData.WorkerId == default(Guid))
            {
                throw new InvalidOperationException($"A worker attached to queue {reportData.SourceQueue} failed to report its worker ID when sending a worker report with status {reportData.Status}." +
                                                    "We cannot proceed with evaluating its report. This indicates a serious flaw in the worker.");
            }
            return reportData;
        }

        private void PlaceMessageIntoHoldingQueue(string sourceQueue, Exception exception, Message<object> message)
        {
            var exiledMessage = new ExiledMessage
            {
                SourceQueueIdentifier = sourceQueue,
                Error = exception,
                Message = message,
                ExileEventDateTime = DateTime.Now
            };
            try
            {
                using (var exileConnection = _connectionFactory.ConnectToQueue<ExiledMessage>(GetConfigValue("ExiledMessagesQueueIdentifier")))
                {
                    exileConnection.Write(Message<ExiledMessage>.WithData(exiledMessage));
                }
            }
            catch (Exception writeError)
            {
                var ae = new AggregateException(writeError, exception);
                OnErrorAction(ae);
            }
        }
    }
}
