using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Database.Core.TableReflection.Impl
{
	public class ConnectionStringFactory : IConnectionStringFactory
	{
		private readonly IDictionary<DatabaseType, IConnectionStringFactory> _connectionStringFactories;

		public ConnectionStringFactory()
		{
			_connectionStringFactories = new Dictionary<DatabaseType, IConnectionStringFactory>
			{
				{ DatabaseType.SqlServer, new SqlServerConnectionStringFactory() }
			};
		}

		public string Create(Database database)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database", "Cannot create connection string for null database.");
			}

			IConnectionStringFactory connectionStringFactory;
			if (_connectionStringFactories.TryGetValue(database.DatabaseType, out connectionStringFactory) == false)
			{
				throw new ArgumentException(String.Format("No IConnectionStringFactory defined for database type '{0}'.", database.DatabaseType));
			}

			return connectionStringFactory.Create(database);
		}
	}
}
