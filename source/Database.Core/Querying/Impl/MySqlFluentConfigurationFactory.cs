using NHibernate.Dialect;
using NHibernate.Driver;

namespace Database.Core.Querying.Impl
{
	public class MySqlFluentConfigurationFactory : AbstractFluentConfigurationFactory<MySQL5Dialect, MySqlDataDriver>
	{
		public MySqlFluentConfigurationFactory(IConnectionStringFactory connectionStringFactory) : base(connectionStringFactory)
		{
		}
	}
}
