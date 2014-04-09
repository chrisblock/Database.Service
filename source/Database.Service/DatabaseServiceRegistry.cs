using Database.Core;

using StructureMap.Configuration.DSL;

namespace Database.Service
{
	public class DatabaseServiceRegistry : Registry
	{
		public DatabaseServiceRegistry()
		{
			Scan(scan =>
			{
				scan.AssemblyContainingType<DatabaseServiceRegistry>();

				scan.WithDefaultConventions();
			});

			IncludeRegistry<DatabaseCoreRegistry>();
		}
	}
}
