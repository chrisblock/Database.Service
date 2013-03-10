using System;

using NHibernate;

namespace Database.Core.Querying
{
	public interface ISessionBuilder : IDisposable
	{
		IStatelessSession Build(Database database, Type mappingType);
	}
}
