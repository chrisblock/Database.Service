using NHibernate.Dialect;
using NHibernate.Driver;

namespace Database.Core.Querying.Impl
{
	public class SqlServer2008FluentConfigurationFactory : AbstractFluentConfigurationFactory<MsSql2008Dialect, SqlClientDriver>
	{
		public SqlServer2008FluentConfigurationFactory(IConnectionStringFactory connectionStringFactory) : base(connectionStringFactory)
		{
		}
	}
}
