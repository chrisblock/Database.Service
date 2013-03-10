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

			// in ASP.NET MVC 4, the IDependencyScope instances are per-request, and when using a StructureMap
			//   NestedContainer all transient instances are scoped to that Container
			//For<ISessionBuilder>()
				//.LifecycleIs(Lifecycles.GetLifecycle(InstanceScope.PerRequest));
		}
	}
}
