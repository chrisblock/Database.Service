using FluentNHibernate.Cfg;

namespace Database.Core
{
	public interface IFluentConfigurationFactory
	{
		FluentConfiguration Create(Database database);
	}
}
