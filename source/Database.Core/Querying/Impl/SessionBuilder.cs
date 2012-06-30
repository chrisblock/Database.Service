using System;
using System.Data;

using NHibernate;

namespace Database.Core.Querying.Impl
{
	public class SessionBuilder : ISessionBuilder
	{
		private readonly IFluentConfigurationCache _fluentConfigurationCache;

		// this should only contain one session and session factory, as this object should be per-request (HTTP scoped)
		private ISessionFactory _sessionFactory;
		private ISession _session;
		private ITransaction _transaction;

		public SessionBuilder(IFluentConfigurationCache fluentConfigurationCache)
		{
			_fluentConfigurationCache = fluentConfigurationCache;
		}

		public ISession Build(Database database, Type mappingType)
		{
			if (_sessionFactory == null)
			{
				var configuration = _fluentConfigurationCache.GetConfigurationFor(database, mappingType);

				_sessionFactory = configuration.BuildSessionFactory();
			}

			var result = _session ?? (_session = _sessionFactory.OpenSession());

			_transaction = _session.BeginTransaction(IsolationLevel.ReadCommitted);

			return result;
		}

		public void Dispose()
		{
			try
			{
				_transaction.Commit();
			}
			catch (Exception)
			{
				if ((_transaction.WasCommitted == false) && (_transaction.WasRolledBack == false))
				{
					_transaction.Rollback();
				}

				throw;
			}

			_transaction.Dispose();

			_session.Flush();

			_session.Close();

			_session.Dispose();
			
			_sessionFactory.Close();

			_sessionFactory.Dispose();
		}
	}
}
