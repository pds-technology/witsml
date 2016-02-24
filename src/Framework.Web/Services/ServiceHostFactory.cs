using System;
using System.Web.Http;

namespace PDS.Framework.Web.Services
{
    /// <summary>
    /// Service host factory that provides access to the composition container used for dependency injection.
    /// </summary>
    /// <seealso cref="System.ServiceModel.Activation.ServiceHostFactory" />
    public class ServiceHostFactory : System.ServiceModel.Activation.ServiceHostFactory
    {
        /// <summary>
        /// Creates a <see cref="T:System.ServiceModel.ServiceHost" /> for a specified type of service with a specific base address.
        /// </summary>
        /// <param name="serviceType">Specifies the type of service to host.</param>
        /// <param name="baseAddresses">The <see cref="T:System.Array" /> of type <see cref="T:System.Uri" /> that contains the base addresses for the service hosted.</param>
        /// <returns>A <see cref="T:System.ServiceModel.ServiceHost" /> for the type of service specified with a specific base address.</returns>
        protected override System.ServiceModel.ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            var resolver = GlobalConfiguration.Configuration.DependencyResolver;
            return new ServiceHost(resolver, serviceType, baseAddresses);
        }
    }
}