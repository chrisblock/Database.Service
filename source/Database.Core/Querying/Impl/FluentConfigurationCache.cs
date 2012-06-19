using System;
using System.Collections.Concurrent;

using FluentNHibernate.Cfg;

namespace Database.Core.Querying.Impl
{
	public class FluentConfigurationCache : IFluentConfigurationCache
	{
		private readonly IFluentConfigurationFactory _fluentConfigurationFactory;

		private static readonly ConcurrentDictionary<Type, FluentConfiguration> ConfigurationsByType = new ConcurrentDictionary<Type, FluentConfiguration>();
		private static readonly ConcurrentDictionary<Database, FluentConfiguration> ConfigurationsByDatabase = new ConcurrentDictionary<Database, FluentConfiguration>();

		private static readonly object Locker = new object();

		public FluentConfigurationCache(IFluentConfigurationFactory fluentConfigurationFactory)
		{
			_fluentConfigurationFactory = fluentConfigurationFactory;
		}

		public FluentConfiguration GetConfigurationFor(Database database, Type mappingType)
		{
			if (ConfigurationsByType.ContainsKey(mappingType) == false)
			{
				// TODO: add another lock to lock each dictionary seperately..? may cause deadlock? seems useless since their usage would be nested...
				lock (Locker)
				{
					if (ConfigurationsByType.ContainsKey(mappingType) == false)
					{
						if (ConfigurationsByDatabase.ContainsKey(database) == false)
						{
							ConfigurationsByDatabase[database] = _fluentConfigurationFactory.Create(database);
						}

						var configuration = ConfigurationsByDatabase[database];

						ConfigurationsByType[mappingType] = configuration;

						configuration.Mappings(mappings => mappings.FluentMappings.Add(mappingType));
					}
				}
			}

			return ConfigurationsByType[mappingType];
		}
	}
}
