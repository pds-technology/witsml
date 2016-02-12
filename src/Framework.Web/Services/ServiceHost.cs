using System;
using System.ServiceModel.Description;
using System.Web.Http.Dependencies;

namespace PDS.Framework.Web.Services
{
    public class ServiceHost : System.ServiceModel.ServiceHost
    {
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

        private void ApplyServiceBehaviors(IDependencyResolver resolver)
        {
            foreach (IServiceBehavior serviceBehavior in resolver.GetServices(typeof(IServiceBehavior)))
            {
                Description.Behaviors.Add(serviceBehavior);
            }
        }
    }
}