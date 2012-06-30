using System.Linq;

using Database.Core.TypeBuilding;

namespace Database.Core.Querying
{
	public interface IQueryExecutor
	{
		IQueryable<T> Execute<T>(Database database, EntityTypes types);
	}
}
