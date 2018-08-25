//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.ServiceModel.Description;
using System.Web.Http.Dependencies;

namespace PDS.WITSMLstudio.Framework.Web.Services
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
