using System;

using NHibernate;

namespace Database.Core.Querying
{
	public interface ISessionBuilder : IDisposable
	{
		ISession Build(Database database, Type mappingType);
	}
}
