using System;
using System.Collections.Concurrent;

using FluentNHibernate.Cfg;

namespace Database.Core.Querying.Impl
{
	public class FluentConfigurationCache : IFluentConfigurationCache
	{
		private readonly IFluentConfigurationFactory _fluentConfigurationFactory;

		private static readonly ConcurrentDictionary<Type, FluentConfiguration> ConfigurationCache = new ConcurrentDictionary<Type, FluentConfiguration>();
		private static readonly ConcurrentDictionary<Database, FluentConfiguration> Configurations = new ConcurrentDictionary<Database, FluentConfiguration>();

		private static readonly object Locker = new object();

		public FluentConfigurationCache(IFluentConfigurationFactory fluentConfigurationFactory)
		{
			_fluentConfigurationFactory = fluentConfigurationFactory;
		}

		public FluentConfiguration GetConfigurationFor(Database database, Type mappingType)
		{
			if (ConfigurationCache.ContainsKey(mappingType) == false)
			{
				// TODO: add another lock to lock each dictionary seperately..? may cause deadlock? seems useless since their usage would be nested...
				lock (Locker)
				{
					if (ConfigurationCache.ContainsKey(mappingType) == false)
					{
						if (Configurations.ContainsKey(database) == false)
						{
							Configurations[database] = _fluentConfigurationFactory.Create(database);
						}

						var configuration = Configurations[database];

						ConfigurationCache[mappingType] = configuration;

						configuration.Mappings(mappings => mappings.FluentMappings.Add(mappingType));
					}
				}
			}

			return ConfigurationCache[mappingType];
		}
	}
}
