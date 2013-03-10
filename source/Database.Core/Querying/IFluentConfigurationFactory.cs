using FluentNHibernate.Cfg;

namespace Database.Core.Querying
{
	public interface IFluentConfigurationFactory
	{
		FluentConfiguration Create(Database database);
	}
}
