using Database.Core.Querying.Impl;

using NHibernate.Dialect;
using NHibernate.Driver;

namespace Database.Core
{
	public class SqlServer2008FluentConfigurationFactory : AbstractFluentConfigurationFactory<MsSql2008Dialect, SqlClientDriver>
	{
		public SqlServer2008FluentConfigurationFactory() : base(new ConnectionStringFactory())
		{
		}
	}
}
