using System;
using System.Collections.Generic;

namespace Database.Core.TableReflection.Impl
{
	public class TableReflector : ITableReflector
	{
		private readonly IDictionary<DatabaseType, ITableReflector> _tableReflectors;

		public TableReflector()
		{
			_tableReflectors = new Dictionary<DatabaseType, ITableReflector>
			{
				{ DatabaseType.SqlServer, new SqlServerTableReflector() }
			};
		}

		public TableDefinition GetTableDefinition(Database database, string tableName)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database", "Cannot reflect a table from a null database.");
			}

			if (String.IsNullOrWhiteSpace(tableName))
			{
				throw new ArgumentException(String.Format("'{0}' is not a valid table name.", tableName), "tableName");
			}

			ITableReflector tableReflector;

			if (_tableReflectors.TryGetValue(database.DatabaseType, out  tableReflector) == false)
			{
				throw new ArgumentException(String.Format("No ITableReflector defined for database type '{0}'.", database.DatabaseType));
			}

			return tableReflector.GetTableDefinition(database, tableName);
		}
	}
}
