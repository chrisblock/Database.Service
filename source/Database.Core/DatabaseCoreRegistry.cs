using Database.Core.TypeBuilding;

using StructureMap.Configuration.DSL;

namespace Database.Core
{
	public class DatabaseCoreRegistry : Registry
	{
		public DatabaseCoreRegistry()
		{
			Scan(scan =>
			{
				scan.AssemblyContainingType<DatabaseCoreRegistry>();

				scan.WithDefaultConventions();
			});

			For<IDynamicAssemblyManagerFactory>()
				.Singleton();
		}
	}
}
