using System.Web.Http;

namespace Database.Service.ApplicationStart
{
	public static class RouteConfiguration
	{
		public static void Configure(HttpRouteCollection routes)
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
	}
}
