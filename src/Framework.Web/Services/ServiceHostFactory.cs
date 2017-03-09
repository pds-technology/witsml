//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Web.Http;

namespace PDS.WITSMLstudio.Framework.Web.Services
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
