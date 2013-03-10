using FluentNHibernate.Cfg;

using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;

namespace Database.Core.Querying
{
	public abstract class AbstractFluentConfigurationFactory<TDialect, TDriver> : IFluentConfigurationFactory
		where TDialect : Dialect
		where TDriver : IDriver
	{
		protected IConnectionStringFactory ConnectionStringFactory { get; private set; }

		protected AbstractFluentConfigurationFactory(IConnectionStringFactory connectionStringFactory)
		{
			ConnectionStringFactory = connectionStringFactory;
		}

		public FluentConfiguration Create(Database database)
		{
			var configuration = new Configuration()
				.DataBaseIntegration(db =>
				{
					db.Dialect<TDialect>();
					db.Driver<TDriver>();
					db.ConnectionString = ConnectionStringFactory.Create(database);
				});

			return Fluently.Configure(configuration);
		}
	}
}
