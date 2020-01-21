using System.Web.Http;

namespace WebSocketsTest
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        // ReSharper disable once UnusedMember.Local
        private GlobalTimer _globalTimer = GlobalTimer.Instance;

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
