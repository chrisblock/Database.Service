using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Xml.Linq;

using Database.Core.Querying;

namespace Database.Core.TableReflection.Impl
{
	public class SqlServerTableReflector : ITableReflector
	{
		private static readonly Type OpenGenericNullableType = typeof (Nullable<>);

		private static readonly IDictionary<string, Type> TypeMapping;

		static SqlServerTableReflector()
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

		private static bool IsMultiByteCharacterColumn(string typeName)
		{
			return (typeName == "nchar") || (typeName == "ntext") || (typeName == "nvarchar");
		}

		private readonly IConnectionStringFactory _connectionStringFactory;

		public SqlServerTableReflector(IConnectionStringFactory connectionStringFactory)
		{
			_connectionStringFactory = connectionStringFactory;
		}

		private IDbConnection CreateConnection(Database database)
		{
			var connectionString = _connectionStringFactory.Create(database);

			return new SqlConnection(connectionString);
		}

		private static Type DetermineColumnType(string sqlTypeName, bool isNullable, short length, byte precision, byte scale)
		{
			var len = IsMultiByteCharacterColumn(sqlTypeName)
				? length >> 2
				: length;

			Type result;
			if (TypeMapping.TryGetValue(sqlTypeName, out result) == false)
			{
				throw new ArgumentException(String.Format("Type '{0}' is not a recognized SQL system type.", sqlTypeName));
			}

			if ((result == typeof(string)) && (len == 1))
			{
				result = typeof(char);
			}

			if (result.IsValueType && isNullable)
			{
				result = OpenGenericNullableType.MakeGenericType(result);
			}

			return result;
		}

		public TableDefinition GetTableDefinition(Database database, string tableName)
		{
			var columns = new List<ColumnDefinition>();

			using (var connection = CreateConnection(database))
			{
				connection.Open();

				using (var command = connection.CreateCommand())
				{
					var commandTextBuilder = new StringBuilder();

					commandTextBuilder.AppendLine("SELECT");
					commandTextBuilder.AppendLine("      [columns].[name] AS [Name]");
					commandTextBuilder.AppendLine("    , [types].[name] AS [Type]");
					commandTextBuilder.AppendLine("    , [columns].[max_length] AS [Length]");
					commandTextBuilder.AppendLine("    , [types].[precision] AS [Precision]");
					commandTextBuilder.AppendLine("    , [types].[scale] AS [Scale]");
					commandTextBuilder.AppendLine("    , ISNULL([indexes].[is_primary_key], 0) AS [IsPrimaryKey]");
					commandTextBuilder.AppendLine("    , [columns].[is_nullable] AS [IsNullable]");
					commandTextBuilder.AppendLine("FROM [sys].[columns] AS [columns]");
					commandTextBuilder.AppendLine("LEFT OUTER JOIN [sys].[types] AS [types] ON [columns].[system_type_id] = [types].[system_type_id]");
					commandTextBuilder.AppendLine("LEFT OUTER JOIN [sys].[index_columns] AS [i_columns] ON [columns].[object_id] = [i_columns].[object_id] AND [columns].[column_id] = [i_columns].[column_id]");
					commandTextBuilder.AppendLine("LEFT OUTER JOIN [sys].[indexes] AS [indexes] ON [i_columns].[object_id] = [indexes].[object_id] AND [indexes].[is_primary_key] = 1");
					commandTextBuilder.AppendLine("WHERE [columns].[object_id] = OBJECT_ID(@tableName);");

					command.CommandText = commandTextBuilder.ToString();

					command.AddParameter("tableName", tableName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var columnName = (string)reader["Name"];
							var type = (string)reader["Type"];
							var length = (short) reader["Length"];
							var precision = (byte) reader["Precision"];
							var scale = (byte)reader["Scale"];
							var isPrimaryKey = (bool)reader["IsPrimaryKey"];
							var isNullable = (bool)reader["IsNullable"];

							var columnType = DetermineColumnType(type, isNullable, length, precision, scale);

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
	}
}
