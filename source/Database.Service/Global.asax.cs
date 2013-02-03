﻿using System.Web;
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
