using System.Web.Http;

using Database.Service.ApplicationStart;

using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof (RouteConfigurationBootstrapper), "Start", Order = 2)]

namespace Database.Service.ApplicationStart
{
	public static class RouteConfigurationBootstrapper
	{
		private static void RegisterRoutes(HttpRouteCollection routes)
		{
			routes.MapHttpRoute(
				name: "ServerInstanceDatabaseTable",
				routeTemplate: "{databaseType}/{serverName}/{instanceName}/{databaseName}/{tableName}",
				defaults: new { controller = "Database" });

			routes.MapHttpRoute(
				name: "ServerDatabaseTable",
				routeTemplate: "{databaseType}/{serverName}/{databaseName}/{tableName}",
				defaults: new { controller = "Database" });

			routes.MapHttpRoute(
				name: "DatabaseTable",
				routeTemplate: "{databaseType}/{databaseName}/{tableName}",
				defaults: new { controller = "Database" });
		}

		public static void Start()
		{
			RegisterRoutes(GlobalConfiguration.Configuration.Routes);
		}
	}
}
