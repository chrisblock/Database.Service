using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using Database.Core.Querying;

using MySql.Data.MySqlClient;

namespace Database.Core.TableReflection.Impl
{
	public class MySqlTableReflector : ITableReflector
	{
		private static readonly Type OpenGenericNullableType = typeof (Nullable<>);

		private static bool IsMultiByteCharacterColumn(string typeName)
		{
			return (typeName == "nchar") || (typeName == "ntext") || (typeName == "nvarchar");
		}

		private readonly ITypeNameMapper _typeNameMapper;
		private readonly IConnectionStringFactory _connectionStringFactory;

		public MySqlTableReflector(IConnectionStringFactory connectionStringFactory, ITypeNameMapper typeNameMapper)
		{
			_connectionStringFactory = connectionStringFactory;
			_typeNameMapper = typeNameMapper;
		}

		private IDbConnection GetConnection(Database database)
		{
			var connectionString = _connectionStringFactory.Create(database);

			return new MySqlConnection(connectionString);
		}

		private Type DetermineColumnType(DatabaseType databaseType, string mySqlTypeName, bool isNullable, short length, byte precision, byte scale)
		{
			var len = IsMultiByteCharacterColumn(mySqlTypeName)
				? length >> 2
				: length;

			var result = _typeNameMapper.GetType(databaseType, mySqlTypeName);

			if ((result == typeof (string)) && (len == 1))
			{
				result = typeof (char);
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

			using (var connection = GetConnection(database))
			{
				connection.Open();

				using (var command = connection.CreateCommand())
				{
					var commandTextBuilder = new StringBuilder();

					commandTextBuilder.AppendLine("SELECT");
					commandTextBuilder.AppendLine("      COLUMNS.COLUMN_NAME AS Name");
					commandTextBuilder.AppendLine("    , COLUMNS.DATA_TYPE AS Type");
					commandTextBuilder.AppendLine("    , IFNULL(COLUMNS.CHARACTER_MAXIMUM_LENGTH, 0) AS Length");
					commandTextBuilder.AppendLine("    , IFNULL(COLUMNS.NUMERIC_PRECISION, 0) AS `Precision`");
					commandTextBuilder.AppendLine("    , IFNULL(COLUMNS.NUMERIC_SCALE, 0) AS Scale");
					commandTextBuilder.AppendLine("    , CASE WHEN KEY_COLUMN_USAGE.CONSTRAINT_NAME IS NULL THEN 0 ELSE 1 END AS IsPrimaryKey");
					commandTextBuilder.AppendLine("    , CASE COLUMNS.IS_NULLABLE WHEN 'YES' THEN 1 ELSE 0 END AS IsNullable");
					commandTextBuilder.AppendLine("FROM INFORMATION_SCHEMA.COLUMNS");
					commandTextBuilder.AppendLine("LEFT OUTER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS ON COLUMNS.TABLE_SCHEMA = TABLE_CONSTRAINTS.TABLE_SCHEMA AND COLUMNS.TABLE_NAME = TABLE_CONSTRAINTS.TABLE_NAME AND TABLE_CONSTRAINTS.CONSTRAINT_TYPE = 'PRIMARY KEY'");
					commandTextBuilder.AppendLine("LEFT OUTER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ON COLUMNS.TABLE_CATALOG = KEY_COLUMN_USAGE.TABLE_CATALOG AND TABLE_CONSTRAINTS.TABLE_SCHEMA = KEY_COLUMN_USAGE.TABLE_SCHEMA AND TABLE_CONSTRAINTS.TABLE_NAME = KEY_COLUMN_USAGE.TABLE_NAME AND TABLE_CONSTRAINTS.CONSTRAINT_NAME = KEY_COLUMN_USAGE.CONSTRAINT_NAME AND COLUMNS.COLUMN_NAME = KEY_COLUMN_USAGE.COLUMN_NAME");
					commandTextBuilder.AppendLine("WHERE COLUMNS.TABLE_NAME = @tableName");
					commandTextBuilder.AppendLine("AND COLUMNS.TABLE_CATALOG = @databaseName");

					command.CommandText = commandTextBuilder.ToString();

					command.AddParameter("tableName", tableName);
					command.AddParameter("databaseName", database.DatabaseName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var columnName = (string) reader["Name"];
							var type = (string) reader["Type"];
							var length = (short) reader["Length"];
							var precision = (byte) reader["Precision"];
							var scale = (byte) reader["Scale"];
							var isPrimaryKey = (bool) reader["IsPrimaryKey"];
							var isNullable = (bool) reader["IsNullable"];

							var columnType = DetermineColumnType(database.DatabaseType, type, isNullable, length, precision, scale);

							var column = new ColumnDefinition
							{
								Name = columnName,
								Type = columnType,
								IsPrimaryKeyColumn = isPrimaryKey
							};

							columns.Add(column);
						}

						reader.Close();
					}
				}

				connection.Close();
			}

			var result = new TableDefinition
			{
				Name = tableName,
				Columns = columns
			};

			return result;
		}
	}
}
