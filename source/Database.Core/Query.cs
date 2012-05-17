using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Database.Core
{
	public class Query
	{
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

			var columns = ReflectTable().ToList();

			var types = DynamicAssemblyManager.BuildTypesForTable(tableName, columns);

			_entityType = types.Item1;
			_mappingType = types.Item2;
		}

		private IEnumerable<ColumnDefinition> ReflectTable()
		{
			var columns = new List<ColumnDefinition>();

			using (var connection = _connect.Create())
			{
				connection.Open();

				using (var command = connection.CreateCommand())
				{
					command.CommandText = "SELECT [columns].[name] AS [Name], [types].[name] AS [Type], ISNULL([indexes].[is_primary_key], 0) AS [IsKey] FROM [sys].[columns] AS [columns] LEFT OUTER JOIN [sys].[types] AS [types] ON [columns].[system_type_id] = [types].[system_type_id] LEFT OUTER JOIN [sys].[index_columns] AS [i_columns] ON [columns].[object_id] = [i_columns].[object_id] AND [columns].[column_id] = [i_columns].[column_id] LEFT OUTER JOIN [sys].[indexes] AS [indexes] ON [i_columns].[object_id] = [indexes].[object_id] WHERE [columns].[object_id] = OBJECT_ID(@tableName);";

					command.AddParameter("tableName", _tableName);

					using(var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var columnName = (string)reader["Name"];
							var type = (string)reader["Type"];
							var isPrimaryKey = (bool) reader["IsKey"];

							Type columnType;
							if (TypeMapping.TryGetValue(type, out columnType) == false)
							{
								throw new ArgumentException(String.Format("Type '{0}' is not a recognized SQL system type.", type));
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

			return columns;
		}

		public Type GetEntityType()
		{
			return _entityType;
		}

		public Type GetMappingType()
		{
			return _mappingType;
		}
	}
}
