using System.Web;
using System.Web.Http;

using Database.Service.ApplicationStart;

using StructureMap;

[assembly: PreApplicationStartMethod(typeof(StructureMapBootstrapper), "Start")]

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
