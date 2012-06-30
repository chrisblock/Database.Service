using Database.Core;
using Database.Core.TypeBuilding;
using Database.Core.TypeBuilding.Impl;

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

			For<IDynamicAssemblyManager>()
				.Use(() => new DynamicAssemblyManagerFactory().Create("Database.DynamicMappings"));

			// in ASP.NET MVC 4, the IDependencyScope instances are per-request, and when using a StructureMap
			//   NestedContainer all transient instances are NestedContainer scoped
			//For<ISessionBuilder>()
				//.LifecycleIs(Lifecycles.GetLifecycle(InstanceScope.PerRequest));
		}
	}
}
