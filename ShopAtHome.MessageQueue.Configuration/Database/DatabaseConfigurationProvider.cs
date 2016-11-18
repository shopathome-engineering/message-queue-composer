using System.Collections.Generic;
using System.Linq;
using ShopAtHome.DapperORMCore;
using ShopAtHome.MessageQueue.Consumers.Configuration;

namespace ShopAtHome.MessageQueue.Configuration.Database
{
    public class DatabaseConfigurationProvider : IConfigurationProvider
    {
        private readonly IDBProvider _dbProvider;
        private readonly IConnectionStringConfig _connectionStringConfig;

        public DatabaseConfigurationProvider(IDBProvider dbProvider, IConnectionStringConfig connectionStringConfig)
        {
            _dbProvider = dbProvider;
            _connectionStringConfig = connectionStringConfig;
        }

        public IQueueConfiguration GetQueueConfiguration(string queueIdentifier)
        {
            using (var db = _dbProvider.Connect(_connectionStringConfig.TransactionProcessing))
            {
                var ident = DebugModeStringHandler.UnmakeDebugIdentifierValue(queueIdentifier);
                return _dbProvider.Select<PocoQueueConfiguration>(db, x => x.QueueIdentifier == ident).FirstOrDefault();
            }
        }

        public IWorkerConfiguration GetWorkerConfiguration(string queueIdentifier)
        {
            using (var db = _dbProvider.Connect(_connectionStringConfig.TransactionProcessing))
            {
                var ident = DebugModeStringHandler.UnmakeDebugIdentifierValue(queueIdentifier);
                return _dbProvider.Select<PocoWorkerConfiguration>(db, x => x.SourceQueue == ident).FirstOrDefault();
            }
        }

        public IEnumerable<IQueueConfiguration> GetQueueConfigurations(int systemIdentifier)
        {
            using (var db = _dbProvider.Connect(_connectionStringConfig.TransactionProcessing))
            {
                return _dbProvider.Select<PocoQueueConfiguration>(db, x => x.SystemIdentifier == systemIdentifier);
            }
        }

        public IListenerConfiguration GetListenerConfiguration(string queueIdentifier)
        {
            using (var db = _dbProvider.Connect(_connectionStringConfig.TransactionProcessing))
            {
                var ident = DebugModeStringHandler.UnmakeDebugIdentifierValue(queueIdentifier);
                return _dbProvider.Select<PocoListenerConfiguration>(db, x => x.SourceQueue == ident).FirstOrDefault();
            }
        }

        public IExchangeConfiguration GetExchangeConfiguration(string exchangeIdentifier)
        {
            using (var db = _dbProvider.Connect(_connectionStringConfig.TransactionProcessing))
            {
                var ident = DebugModeStringHandler.UnmakeDebugIdentifierValue(exchangeIdentifier);
                return _dbProvider.Select<PocoExchangeConfiguration>(db, x => x.ExchangeIdentifier == ident).FirstOrDefault();
            }
        }
    }
}
