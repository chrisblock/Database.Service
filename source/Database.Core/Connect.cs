using System;

namespace Database.Core
{
	public class Connect
	{
		private readonly Database _database;

		private Connect(Database database)
		{
			_database = database;
		}

		public Query CreateQuery(string tableName)
		{
			var query = new Query(_database, tableName);

			return query;
		}

		public static Connect To(Database database)
		{
			if (String.IsNullOrWhiteSpace(database.ServerName))
			{
				throw new ArgumentException(String.Format("Cannot connect with an invalid server name."));
			}

			if (String.IsNullOrWhiteSpace(database.DatabaseName))
			{
				throw new ArgumentException(String.Format("Cannot connect with an invalid database name."));
			}

			return new Connect(database);
		}

		public static Connect To(string serverName, string databaseName)
		{
			var database = new Database
			{
				ServerName = serverName,
				DatabaseName = databaseName
			};

			return To(database);
		}

		public static Connect To(string databaseName)
		{
			return To(Environment.MachineName, databaseName);
		}

		public override string ToString()
		{
			return _database.ToString();
		}

		public override int GetHashCode()
		{
			return String.Format("Database:{0};", _database).GetHashCode();
		}
	}
}
