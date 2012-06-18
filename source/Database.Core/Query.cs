using System;
using System.Linq;

using Database.Core.TableReflection;
using Database.Core.TableReflection.Impl;

using FluentNHibernate.Cfg;

using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Linq;

namespace Database.Core
{
	public class Query
	{
		private readonly IConnectionStringFactory _connectionStringFactory = new ConnectionStringFactory();
		private readonly ITableReflector _tableReflector = new TableReflector();

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
			var configuration = new Configuration()
				.DataBaseIntegration(x =>
				{
					x.Dialect<MsSql2008Dialect>();
					x.Driver<SqlClientDriver>();
					x.ConnectionString = _connectionStringFactory.Create(_database);
				});

			var sessionFactory = Fluently.Configure(configuration)
				.Mappings(mappings => mappings.FluentMappings.Add(_mappingType))
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
