using System.Web;
using System.Web.Http;

using Database.Service.ApplicationStart;

namespace Database.Service
{
	public class WebApiApplication : HttpApplication
	{
		protected void Application_Start()
		{
			ConfigureApplication(GlobalConfiguration.Configuration);
		}

		private void ConfigureApplication(HttpConfiguration configuration)
		{
			StructureMapConfiguration.Configure(configuration);

			configuration.MapHttpAttributeRoutes();
		}
	}
}
