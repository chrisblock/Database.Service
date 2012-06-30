using System.Linq;

using Database.Core.TypeBuilding;

using NHibernate.Linq;

namespace Database.Core.Querying.Impl
{
	public class QueryExecutor : IQueryExecutor
	{
		private readonly ISessionBuilder _sessionBuilder;

		public QueryExecutor(ISessionBuilder sessionBuilder)
		{
			_sessionBuilder = sessionBuilder;
		}

		public IQueryable<T> Execute<T>(Database database, EntityTypes types)
		{
			var session = _sessionBuilder.Build(database, types.MappingType);

			var result = session.Query<T>();

			return result;
		}
	}
}
