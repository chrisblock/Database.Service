using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;

using Database.Core.Querying;

namespace Database.Core.TableReflection.Impl
{
	public class SqlServerTableReflector : ITableReflector
	{
		private static readonly Type OpenGenericNullableType = typeof (Nullable<>);

		private static readonly Lazy<string> LazyCommandText = new Lazy<string>(BuildCommandText, LazyThreadSafetyMode.ExecutionAndPublication);
		private static string CommandText { get { return LazyCommandText.Value; } }

		private static string BuildCommandText()
		{
			var commandTextBuilder = new StringBuilder();

			commandTextBuilder.AppendLine("SELECT");
			commandTextBuilder.AppendLine("      [columns].[name] AS [Name]");
			commandTextBuilder.AppendLine("    , [types].[name] AS [Type]");
			commandTextBuilder.AppendLine("    , [columns].[max_length] AS [Length]");
			commandTextBuilder.AppendLine("    , [types].[precision] AS [Precision]");
			commandTextBuilder.AppendLine("    , [types].[scale] AS [Scale]");
			commandTextBuilder.AppendLine("    , CONVERT(BIT, [columns].[is_nullable]) AS [IsNullable]");
			commandTextBuilder.AppendLine("    , CONVERT(BIT, [columns].[is_identity]) AS [IsIdentity]");
			commandTextBuilder.AppendLine("    , CONVERT(BIT, CASE WHEN [index_columns].[column_id] IS NULL THEN 0 ELSE 1 END) AS [IsPrimaryKey]");
			commandTextBuilder.AppendLine("FROM [sys].[columns]");
			commandTextBuilder.AppendLine("INNER JOIN [sys].[types]");
			commandTextBuilder.AppendLine("            ON [types].[system_type_id] = [columns].[system_type_id]");
			commandTextBuilder.AppendLine("            AND [types].[user_type_id] = [columns].[user_type_id]");
			commandTextBuilder.AppendLine("LEFT OUTER JOIN [sys].[indexes]");
			commandTextBuilder.AppendLine("            ON [indexes].[object_id] = [columns].[object_id]");
			commandTextBuilder.AppendLine("            AND [indexes].[is_primary_key] = 1");
			commandTextBuilder.AppendLine("LEFT OUTER JOIN [sys].[index_columns]");
			commandTextBuilder.AppendLine("            ON [index_columns].[object_id] = [indexes].[object_id]");
			commandTextBuilder.AppendLine("            AND [index_columns].[index_id] = [indexes].[index_id]");
			commandTextBuilder.AppendLine("            AND [index_columns].[column_id] = [columns].[column_id]");
			commandTextBuilder.AppendLine("WHERE [columns].[object_id] = OBJECT_ID(@tableName);");

			return commandTextBuilder.ToString();
		}

		private static bool IsMultiByteCharacterColumn(string typeName)
		{
			return (typeName == "nchar") || (typeName == "ntext") || (typeName == "nvarchar");
		}

		private readonly ITypeNameMapper _typeNameMapper;
		private readonly IConnectionStringFactory _connectionStringFactory;

		public SqlServerTableReflector(IConnectionStringFactory connectionStringFactory, ITypeNameMapper typeNameMapper)
		{
			_connectionStringFactory = connectionStringFactory;
			_typeNameMapper = typeNameMapper;
		}

		private IDbConnection CreateConnection(Database database)
		{
			var connectionString = _connectionStringFactory.Create(database);

			return new SqlConnection(connectionString);
		}

		private Type DetermineColumnType(DatabaseType databaseType, string sqlTypeName, bool isNullable, short length, byte precision, byte scale)
		{
			var len = IsMultiByteCharacterColumn(sqlTypeName)
				? length >> 2
				: length;

			var result = _typeNameMapper.GetType(databaseType, sqlTypeName);

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

			using (var connection = CreateConnection(database))
			{
				connection.Open();

				using (var command = connection.CreateCommand(CommandText))
				{
					command.AddParameter("tableName", tableName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var columnName = (string) reader["Name"];
							var type = (string) reader["Type"];
							var length = (short) reader["Length"];
							var precision = (byte) reader["Precision"];
							var scale = (byte) reader["Scale"];
							var isNullable = (bool) reader["IsNullable"];
							var isIdentity = (bool) reader["IsIdentity"];
							var isPrimaryKey = (bool) reader["IsPrimaryKey"];

							var columnType = DetermineColumnType(database.DatabaseType, type, isNullable, length, precision, scale);

							var column = new ColumnDefinition
							{
								Name = columnName,
								Type = columnType,
								Length = length,
								Scale = scale,
								Precision = precision,
								IsNullable = isNullable,
								IsIdentity = isIdentity,
								IsPrimaryKeyColumn = isPrimaryKey
							};

							columns.Add(column);
						}

						reader.Close();
					}
				}

				connection.Close();
			}

			if (columns.Any() == false)
			{
				throw new ArgumentException(String.Format("Error when querying database '{0}' on server '{1}'. Either the table '{2}' does not exist, or it has no columns.", database.DatabaseName, database.ServerName, tableName));
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
