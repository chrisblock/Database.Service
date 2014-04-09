using System.Web.Http;

using StructureMap;

namespace Database.Service.ApplicationStart
{
	public static class StructureMapConfiguration
	{
		public static void Configure(HttpConfiguration configuration)
		{
			ObjectFactory.Initialize(init => init.AddRegistry<DatabaseServiceRegistry>());

			configuration.DependencyResolver = new StructureMapDependencyResolver();
		}
	}
}
