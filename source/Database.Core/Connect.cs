using System;
using System.Data.SqlClient;

namespace Database.Core
{
	public class Connect
	{
		private readonly SqlConnectionStringBuilder _connectionStringBuilder;

		private Connect(SqlConnectionStringBuilder connectionStringBuilder)
		{
			_connectionStringBuilder = connectionStringBuilder;
		}

		public Query CreateQuery(string tableName)
		{
			var query = new Query(this, tableName);

			return query;
		}

		public SqlConnection Create()
		{
			return new SqlConnection(_connectionStringBuilder.ToString());
		}

		public string GetConnectionString()
		{
			return _connectionStringBuilder.ToString();
		}

		public static Connect To(SqlConnectionStringBuilder connectionStringBuilder)
		{
			if (String.IsNullOrWhiteSpace(connectionStringBuilder.DataSource))
			{
				throw new ArgumentException(String.Format("Cannot connect with an invalid server name."));
			}

			if (String.IsNullOrWhiteSpace(connectionStringBuilder.InitialCatalog))
			{
				throw new ArgumentException(String.Format("Cannot connect with an invalid database name."));
			}

			return new Connect(connectionStringBuilder);
		}

		public static Connect To(string serverName, string databaseName)
		{
			var connectionStringBuilder = new SqlConnectionStringBuilder
			{
				DataSource = serverName,
				InitialCatalog = databaseName,
				IntegratedSecurity = true
			};

			return To(connectionStringBuilder);
		}

		public static Connect To(string databaseName)
		{
			return To("localhost", databaseName);
		}

		public override string ToString()
		{
			return GetConnectionString();
		}

		public override int GetHashCode()
		{
			return String.Format("ConnectionString:{0};", GetConnectionString()).GetHashCode();
		}
	}
}
