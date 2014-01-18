using System.Web.Http;

using StructureMap;

namespace Database.Service.ApplicationStart
{
	public static class StructureMapConfiguration
	{
		public static void Configure()
		{
			ObjectFactory.Initialize(init => init.AddRegistry<DatabaseServiceRegistry>());

			GlobalConfiguration.Configuration.DependencyResolver = new StructureMapDependencyResolver();
		}
	}
}
