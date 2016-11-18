using System;
using AutoMapper;
using ShopAtHome.DapperORMCore;
using IMapperConfiguration = ShopAtHome.DapperORMCore.IMapperConfiguration;

namespace ShopAtHome.MessageQueue.Configuration
{
    public class MQConfigurationMapper : IObjectMappingProvider
    {
        public void ApplyMappings(IMapperConfiguration configuration)
        {
            ListenerConfigurationMapping(configuration);
            QueueConfigurationMapping(configuration);
            WorkerConfigurationMapping(configuration);
            ExchangeConfigurationMapping(configuration);
            Mapper.AssertConfigurationIsValid();
        }

        private static void ListenerConfigurationMapping(IMapperConfiguration configuration)
        {
            configuration.CreateMap<dynamic, PocoListenerConfiguration>().ConvertUsing(HydrateListenerConfiguration);
        }

        private static PocoListenerConfiguration HydrateListenerConfiguration(dynamic queryResult)
        {
            return new PocoListenerConfiguration
            {
                ActorType = Type.GetType(queryResult.ActorType, true),
                SourceQueue = DebugModeStringHandler.MakeDebugIdentifierValue(queryResult.SourceQueue)
            };
        }

        private static void WorkerConfigurationMapping(IMapperConfiguration configuration)
        {
            configuration.CreateMap<dynamic, PocoWorkerConfiguration>().ConvertUsing(HydrateWorkerConfiguration);
        }

        private static PocoWorkerConfiguration HydrateWorkerConfiguration(dynamic queryResult)
        {
            return new PocoWorkerConfiguration
            {
                ActorType = Type.GetType(queryResult.ActorType, true),
                NextQueue = DebugModeStringHandler.MakeDebugIdentifierValue(queryResult.NextQueue),
                SourceQueue = DebugModeStringHandler.MakeDebugIdentifierValue(queryResult.SourceQueue)
            };
        }

        private static void QueueConfigurationMapping(IMapperConfiguration configuration)
        {
            configuration.CreateMap<dynamic, PocoQueueConfiguration>().ConvertUsing(HydrateQueueConfiguration);
        }

        private static PocoQueueConfiguration HydrateQueueConfiguration(dynamic data)
        {
            return new PocoQueueConfiguration
            {
                ExchangeIdentifier = DebugModeStringHandler.MakeDebugIdentifierValue(data.ExchangeIdentifier),
                QueueIdentifier = DebugModeStringHandler.MakeDebugIdentifierValue(data.QueueIdentifier),
                PollingInterval = data.PollingInterval,
                QueueReadInterval = data.QueueReadInterval,
                QueueReadMessageCountThreshold = data.QueueReadMessageCountThreshold,
                RoutingKey = data.RoutingKey,
                TargetTimeToClearQueue = data.TargetTimeToClearQueue,
                SystemIdentifier = data.SystemIdentifier
            };
        }

        private static void ExchangeConfigurationMapping(IMapperConfiguration configuration)
        {
            configuration.CreateMap<dynamic, PocoExchangeConfiguration>().ConvertUsing(HydrateExchangeConfiguration);
        }

        private static PocoExchangeConfiguration HydrateExchangeConfiguration(dynamic data)
        {
            return new PocoExchangeConfiguration
            {
                AlternateExchangeIdentifier = DebugModeStringHandler.MakeDebugIdentifierValue(data.AlternateExchangeIdentifier),
                ExchangeIdentifier = DebugModeStringHandler.MakeDebugIdentifierValue(data.ExchangeIdentifier),
                RoutingKeyFormat = data.RoutingKeyFormat,
                Type = data.Type
            };
        }
    }
}
