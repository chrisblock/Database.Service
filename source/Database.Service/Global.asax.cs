﻿using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Database.Service
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class WebApiApplication : HttpApplication
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.MapHttpRoute("ServerInstanceDatabaseTable", "{serverName}/{instanceName}/{databaseName}/{tableName}", new { controller = "Database" });
			routes.MapHttpRoute("ServerDatabaseTable", "{serverName}/{databaseName}/{tableName}", new { controller = "Database" });
			routes.MapHttpRoute("DatabaseTable", "{databaseName}/{tableName}", new { controller = "Database" });

			routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional }
			);
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
