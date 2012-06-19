using System;
using System.Linq;

using Database.Core.Querying;
using Database.Core.Querying.Impl;
using Database.Core.TableReflection;
using Database.Core.TableReflection.Impl;

using NHibernate;
using NHibernate.Linq;

namespace Database.Core
{
	public class Query
	{
		private readonly ITableReflector _tableReflector = new TableReflector();
		private readonly IFluentConfigurationCache _fluentConfigurationCache = new FluentConfigurationCache(new FluentConfigurationFactory());

		private readonly Database _database;
		private readonly string _tableName;
		private readonly Type _entityType;
		private readonly Type _mappingType;

		public Query(Database database, string tableName)
		{
			_database = database;
			_tableName = tableName;

			var tableDefinition = _tableReflector.GetTableDefinition(_database, _tableName);

			var types = DynamicAssemblyManager.BuildTypesForTable(tableDefinition);

			_entityType = types.Item1;
			_mappingType = types.Item2;
		}

		public Type GetEntityType()
		{
			return _entityType;
		}

		public Type GetMappingType()
		{
			return _mappingType;
		}

		private ISessionFactory BuildSessionFactory()
		{
			var configuration = _fluentConfigurationCache.GetConfigurationFor(_database, _mappingType);

			var sessionFactory = configuration
				.BuildSessionFactory();

			return sessionFactory;
		}

		public IQueryable<T> Execute<T>() where T : class
		{
			IQueryable<T> result;

			using (var sessionFactory = BuildSessionFactory())
			{
				using (var session = sessionFactory.OpenStatelessSession())
				{
					// TODO: some kind of per-request pattern for unit-of-work management so the actual NHibernate IQueryable can be returned
					result = session.Query<T>()
						.ToList()
						.AsQueryable();
				}
			}

			return result;
		}
	}
}
