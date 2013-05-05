using System;
using System.Collections.Concurrent;

using FluentNHibernate.Cfg;

namespace Database.Core.Querying.Impl
{
	public class FluentConfigurationCache : IFluentConfigurationCache
	{
		private static readonly ConcurrentDictionary<Type, FluentConfiguration> ConfigurationsByType = new ConcurrentDictionary<Type, FluentConfiguration>();
		private static readonly ConcurrentDictionary<Database, FluentConfiguration> ConfigurationsByDatabase = new ConcurrentDictionary<Database, FluentConfiguration>();

		private static readonly object Locker = new object();

		private readonly IFluentConfigurationFactory _fluentConfigurationFactory;

		public FluentConfigurationCache(IFluentConfigurationFactory fluentConfigurationFactory)
		{
			_fluentConfigurationFactory = fluentConfigurationFactory;
		}

		public FluentConfiguration GetConfigurationFor(Database database, Type mappingType)
		{
			// storing the configurations by type keeps us from adding the same mapping to the configuration twice
			if (ConfigurationsByType.ContainsKey(mappingType) == false)
			{
				lock (Locker)
				{
					if (ConfigurationsByType.ContainsKey(mappingType) == false)
					{
						// storing the configurations by database keeps us from re-generating the configuration all together
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
