using System.Web;
using System.Web.Http;

using Database.Service.ApplicationStart;

namespace Database.Service
{
	public class WebApiApplication : HttpApplication
	{
		protected void Application_Start()
		{
			StructureMapConfiguration.Configure();
			RouteConfiguration.Configure(GlobalConfiguration.Configuration.Routes);
		}
	}
}
