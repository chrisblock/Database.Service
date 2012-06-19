using System;

using FluentNHibernate.Cfg;

namespace Database.Core.Querying
{
	public interface IFluentConfigurationCache
	{
		FluentConfiguration GetConfigurationFor(Database database, Type mappingType);
	}
}
