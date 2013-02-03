using NHibernate.Dialect;
using NHibernate.Driver;

namespace Database.Core.Querying.Impl
{
	public class SqlServer2008FluentConfigurationFactory : AbstractFluentConfigurationFactory<MsSql2008Dialect, SqlClientDriver>
	{
		public override DatabaseType CompatibleType { get { return DatabaseType.SqlServer; } }

		public SqlServer2008FluentConfigurationFactory(IConnectionStringFactory connectionStringFactory) : base(connectionStringFactory)
		{
		}
	}
}
