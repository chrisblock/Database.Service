namespace Database.Core.TableReflection.Impl
{
	public class SqlServerConnectionStringFactory : IConnectionStringFactory
	{
		public string Create(Database database)
		{
			var connectionStringBuilder = new System.Data.SqlClient.SqlConnectionStringBuilder
			{
				DataSource = database.ServerName,
				InitialCatalog = database.DatabaseName,
				IntegratedSecurity = true
			};

			return connectionStringBuilder.ToString();
		}
	}
}
