using MySql.Data.MySqlClient;

namespace Database.Core.Querying.Impl
{
	public class MySqlConnectionStringFactory : IConnectionStringFactory
	{
		public string Create(Database database)
		{
			var connectionStringBuilder = new MySqlConnectionStringBuilder
			{
				Server = database.ServerName,
				Database = database.DatabaseName,
				IntegratedSecurity = true
			};

			return connectionStringBuilder.ToString();
		}
	}
}
