using System.Web;
using System.Web.Mvc;

namespace Database.Service
{
	public class WebApiApplication : HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
		}
	}
}
