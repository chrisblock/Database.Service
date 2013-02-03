using System.Data.SqlClient;

namespace Database.Core.Querying.Impl
{
	public class SqlServerConnectionStringFactory : IConnectionStringFactory
	{
		public string Create(Database database)
		{
			var connectionStringBuilder = new SqlConnectionStringBuilder
			{
				DataSource = database.ServerName,
				InitialCatalog = database.DatabaseName,
				IntegratedSecurity = true
			};

			return connectionStringBuilder.ToString();
		}
	}
}
