using System;
using System.Collections.Generic;

using FluentNHibernate.Cfg;

namespace Database.Core.Querying.Impl
{
	public class FluentConfigurationFactory : IFluentConfigurationFactory
	{
		private readonly IDictionary<DatabaseType, IFluentConfigurationFactory> _configurationFactories;

		public FluentConfigurationFactory()
		{
			_configurationFactories = new Dictionary<DatabaseType, IFluentConfigurationFactory>
			{
				{ DatabaseType.SqlServer, new SqlServer2008FluentConfigurationFactory() }
			};
		}

		public DatabaseType CompatibleType
		{
			get { throw new NotImplementedException(); }
		}

		public FluentConfiguration Create(Database database)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}

			IFluentConfigurationFactory fluentConfigurationFactory;
			if (_configurationFactories.TryGetValue(database.DatabaseType, out fluentConfigurationFactory) == false)
			{
				throw new ArgumentException(String.Format("No IFluentConfigurationFactory defined for database type '{0}'.", database.DatabaseType));
			}

			return fluentConfigurationFactory.Create(database);
		}
	}
}
