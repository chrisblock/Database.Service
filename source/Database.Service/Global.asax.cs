using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Database.Service
{
	public class WebApiApplication : HttpApplication
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}

		public static void RegisterRoutes(RouteCollection routes)
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

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();

			RegisterGlobalFilters(GlobalFilters.Filters);
			RegisterRoutes(RouteTable.Routes);

			BundleTable.Bundles.EnableDefaultBundles();
		}
	}
}
