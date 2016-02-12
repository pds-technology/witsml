using System;
using System.Web.Http;

namespace PDS.Framework.Web.Services
{
    public class ServiceHostFactory : System.ServiceModel.Activation.ServiceHostFactory
    {
        protected override System.ServiceModel.ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            var resolver = GlobalConfiguration.Configuration.DependencyResolver;
            return new ServiceHost(resolver, serviceType, baseAddresses);
        }
    }
}