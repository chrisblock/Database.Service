using FluentNHibernate.Cfg;

namespace Database.Core.Querying
{
	public interface IFluentConfigurationFactory
	{
		DatabaseType CompatibleType { get; }

		FluentConfiguration Create(Database database);
	}
}
