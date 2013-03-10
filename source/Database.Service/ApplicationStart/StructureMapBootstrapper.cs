using System.Web.Http;

using Database.Service.ApplicationStart;

using StructureMap;

using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof (StructureMapBootstrapper), "Start", Order = 1)]

namespace Database.Service.ApplicationStart
{
	public static class StructureMapBootstrapper
	{
		public static void Start()
		{
			ObjectFactory.Initialize(init => init.AddRegistry<DatabaseServiceRegistry>());

			GlobalConfiguration.Configuration.DependencyResolver = new StructureMapDependencyResolver();
		}
	}
}
