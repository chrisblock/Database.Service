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
		private IStatelessSession _session;
		private ITransaction _transaction;

		public SessionBuilder(IFluentConfigurationCache fluentConfigurationCache)
		{
			_fluentConfigurationCache = fluentConfigurationCache;
		}

		public IStatelessSession Build(Database database, Type mappingType)
		{
			if (_transaction == null)
			{
				if (_session == null)
				{
					if (_sessionFactory == null)
					{
						var configuration = _fluentConfigurationCache.GetConfigurationFor(database, mappingType);

						_sessionFactory = configuration.BuildSessionFactory();
					}

					_session = _sessionFactory.OpenStatelessSession();
				}

				_transaction = _session.BeginTransaction(IsolationLevel.ReadUncommitted);
			}

			return _session;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~SessionBuilder()
		{
			Dispose(false);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_transaction != null)
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
					finally
					{
						_transaction.Dispose();

						CloseSession();

						CloseSessionFactory();
					}
				}
				else
				{
					CloseSession();

					CloseSessionFactory();
				}
			}
		}

		private void CloseSession()
		{
			if (_session != null)
			{
				_session.Close();

				_session.Dispose();

				_session = null;
			}
		}

		private void CloseSessionFactory()
		{
			if (_sessionFactory != null)
			{
				_sessionFactory.Close();

				_sessionFactory.Dispose();

				_sessionFactory = null;
			}
		}
	}
}
