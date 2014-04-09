using System.Web;
using System.Web.Http;

using Database.Service.ApplicationStart;

namespace Database.Service
{
	public class WebApiApplication : HttpApplication
	{
		protected void Application_Start()
		{
			GlobalConfiguration.Configure(ConfigureApplication);
		}

		private void ConfigureApplication(HttpConfiguration configuration)
		{
			StructureMapConfiguration.Configure(configuration);

			configuration.MapHttpAttributeRoutes();
		}
	}
}
