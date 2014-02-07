using System.Web.Http;

namespace Database.Service.ApplicationStart
{
	public static class RouteConfiguration
	{
		public static void Configure(HttpRouteCollection routes)
		{
			routes.MapHttpRoute(
				name: "ServerInstanceDatabaseTable",
				routeTemplate: "{serverType}/{serverName}/{instanceName}/{databaseName}/{tableName}",
				defaults: new { controller = "Database" });

			routes.MapHttpRoute(
				name: "ServerDatabaseTable",
				routeTemplate: "{serverType}/{serverName}/{databaseName}/{tableName}",
				defaults: new { controller = "Database" });

			routes.MapHttpRoute(
				name: "DatabaseTable",
				routeTemplate: "{serverType}/{databaseName}/{tableName}",
				defaults: new { controller = "Database" });
		}
	}
}
