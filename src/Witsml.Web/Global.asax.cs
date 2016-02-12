using System.IO;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using log4net.Config;
using PDS.Framework.Web;
using PDS.Witsml.Server;

namespace PDS.Witsml.Web
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));
            ContainerConfiguration.Register(Server.MapPath("~/bin"));

            // pre-init IWitsmlStore dependencies
            var store = GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IWitsmlStore)) as IWitsmlStore;
            store.WMLS_GetCap(new WMLS_GetCapRequest());
        }
    }
}
