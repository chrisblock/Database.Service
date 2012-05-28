using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

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
		private static readonly Type OpenGenericNullableType = typeof (Nullable<>);

		private static readonly IDictionary<string, Type> TypeMapping;

		static Query()
		{
			TypeMapping = new Dictionary<string, Type>
			{
				{ "bigint", typeof(long) },
				{ "binary", typeof(byte[]) },
				{ "bit", typeof(bool) },
				{ "char", typeof(string) },
				{ "date", typeof(DateTime) },
				{ "datetime", typeof(DateTime) },
				{ "datetimeoffset", typeof(DateTimeOffset) },
				{ "decimal", typeof(decimal) },
				{ "float", typeof(double) },
				{ "image", typeof(byte[]) },
				{ "int", typeof(int) },
				{ "money", typeof(decimal) },
				{ "nchar", typeof(string) },
				{ "ntext", typeof(string) },
				{ "numeric", typeof(decimal) },
				{ "nvarchar", typeof(string) },
				{ "real", typeof(float) },
				{ "rowversion", typeof(byte[]) },
				{ "smalldatetime", typeof(DateTime) },
				{ "smallint", typeof(short) },
				{ "smallmoney", typeof(decimal) },
				{ "sql_variant", typeof(object) },
				{ "text", typeof(string) },
				{ "time", typeof(TimeSpan) },
				{ "timestamp", typeof(byte[]) },
				{ "tinyint", typeof(byte) },
				{ "uniqueidentifier", typeof(Guid) },
				{ "varbinary", typeof(byte[]) },
				{ "varchar", typeof(string) },
				{ "xml", typeof(XDocument) },
			};
		}

		private readonly Connect _connect;
		private readonly string _tableName;
		private readonly Type _entityType;
		private readonly Type _mappingType;

		public Query(Connect connect, string tableName)
		{
			_connect = connect;
			_tableName = tableName;

			var tableDefinition = ReflectTable(_tableName);

			var types = DynamicAssemblyManager.BuildTypesForTable(tableDefinition);

			_entityType = types.Item1;
			_mappingType = types.Item2;
		}

		private TableDefinition ReflectTable(string tableName)
		{
			var columns = new List<ColumnDefinition>();

			using (var connection = _connect.Create())
			{
				connection.Open();

				using (var command = connection.CreateCommand())
				{
					var commandTextBuilder = new StringBuilder();

					commandTextBuilder.AppendLine("SELECT");
					commandTextBuilder.AppendLine("      [columns].[name] AS [Name]");
					commandTextBuilder.AppendLine("    , [types].[name] AS [Type]");
					commandTextBuilder.AppendLine("    , ISNULL([indexes].[is_primary_key], 0) AS [IsPrimaryKey]");
					commandTextBuilder.AppendLine("    , [columns].[is_nullable] AS [IsNullable]");
					commandTextBuilder.AppendLine("FROM [sys].[columns] AS [columns]");
					commandTextBuilder.AppendLine("LEFT OUTER JOIN [sys].[types] AS [types] ON [columns].[system_type_id] = [types].[system_type_id]");
					commandTextBuilder.AppendLine("LEFT OUTER JOIN [sys].[index_columns] AS [i_columns] ON [columns].[object_id] = [i_columns].[object_id] AND [columns].[column_id] = [i_columns].[column_id]");
					commandTextBuilder.AppendLine("LEFT OUTER JOIN [sys].[indexes] AS [indexes] ON [i_columns].[object_id] = [indexes].[object_id]");
					commandTextBuilder.AppendLine("WHERE [columns].[object_id] = OBJECT_ID(@tableName);");

					command.CommandText = commandTextBuilder.ToString();

					command.AddParameter("tableName", tableName);

					using(var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var columnName = (string) reader["Name"];
							var type = (string) reader["Type"];
							var isPrimaryKey = (bool) reader["IsPrimaryKey"];
							var isNullable = (bool)reader["IsNullable"];

							Type columnType;
							if (TypeMapping.TryGetValue(type, out columnType) == false)
							{
								throw new ArgumentException(String.Format("Type '{0}' is not a recognized SQL system type.", type));
							}

							if (columnType.IsValueType && isNullable)
							{
								columnType = OpenGenericNullableType.MakeGenericType(columnType);
							}

							var column = new ColumnDefinition
							{
								Name = columnName,
								Type = columnType,
								IsPrimaryKeyColumn = isPrimaryKey
							};

							columns.Add(column);
						}
					}
				}
				
				connection.Close();
			}

			return new TableDefinition
			{
				Name = tableName,
				Columns = columns
			};
		}

		public Type GetEntityType()
		{
			return _entityType;
		}

		public Type GetMappingType()
		{
			return _mappingType;
		}

		public string GetConnectionString()
		{
			return _connect.GetConnectionString();
		}

		private ISessionFactory BuildSessionFactory()
		{
			var configuration = new Configuration()
				.DataBaseIntegration(x =>
				{
					x.Dialect<MsSql2008Dialect>();
					x.Driver<SqlClientDriver>();
					x.ConnectionString = GetConnectionString();
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
					// TODO: some kind of conversation pattern for unit-of-work management so the actual NHibernate IQueryable can be returned
					result = session.Query<T>()
						.ToList()
						.AsQueryable();
				}
			}

			return result;
		}
	}
}
