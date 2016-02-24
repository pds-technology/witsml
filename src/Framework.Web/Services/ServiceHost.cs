using System;
using System.ServiceModel.Description;
using System.Web.Http.Dependencies;

namespace PDS.Framework.Web.Services
{
    /// <summary>
    /// Service host implementation that provides access to the composition container used for dependency injection.
    /// </summary>
    /// <seealso cref="System.ServiceModel.ServiceHost" />
    public class ServiceHost : System.ServiceModel.ServiceHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHost"/> class.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="baseAddresses">The base addresses.</param>
        public ServiceHost(IDependencyResolver resolver, Type serviceType, Uri[] baseAddresses) : base(serviceType, baseAddresses)
        {
            ApplyServiceBehaviors(resolver);
            ApplyContractBehaviors(resolver);

            foreach (ContractDescription value in ImplementedContracts.Values)
            {
                var contractBehavior = new ServiceContractBehavior(new ServiceInstanceProvider(resolver, value.ContractType));
                value.Behaviors.Add(contractBehavior);
            }
        }

        /// <summary>
        /// Applies the contract behaviors.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        private void ApplyContractBehaviors(IDependencyResolver resolver)
        {
            foreach (IContractBehavior contractBehavior in resolver.GetServices(typeof(IContractBehavior)))
            {
                foreach (ContractDescription value in ImplementedContracts.Values)
                {
                    value.Behaviors.Add(contractBehavior);
                }
            }
        }

        /// <summary>
        /// Applies the service behaviors.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        private void ApplyServiceBehaviors(IDependencyResolver resolver)
        {
            foreach (IServiceBehavior serviceBehavior in resolver.GetServices(typeof(IServiceBehavior)))
            {
                Description.Behaviors.Add(serviceBehavior);
            }
        }
    }
}