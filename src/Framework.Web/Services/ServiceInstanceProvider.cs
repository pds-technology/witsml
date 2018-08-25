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
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Web.Http.Dependencies;

namespace PDS.WITSMLstudio.Framework.Web.Services
{
    /// <summary>
    /// Provider that resolves service instances using the registered composition container.
    /// </summary>
    /// <seealso cref="System.ServiceModel.Dispatcher.IInstanceProvider" />
    public class ServiceInstanceProvider : IInstanceProvider
    {
        private readonly IDependencyResolver _resolver;
        private readonly Type _contractType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceInstanceProvider"/> class.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="contractType">Type of the contract.</param>
        public ServiceInstanceProvider(IDependencyResolver resolver, Type contractType)
        {
            _resolver = resolver;
            _contractType = contractType;
        }

        /// <summary>
        /// Returns a service object given the specified <see cref="T:System.ServiceModel.InstanceContext" /> object.
        /// </summary>
        /// <param name="instanceContext">The current <see cref="T:System.ServiceModel.InstanceContext" /> object.</param>
        /// <param name="message">The message that triggered the creation of a service object.</param>
        /// <returns>
        /// The service object.
        /// </returns>
        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            //var scope = instanceContext.Extensions.Find<ServiceInstanceContextExtension>().GetChildScope(_Resolver);
            //return scope.GetService(_ContractType);
            return _resolver.GetService(_contractType);
        }

        /// <summary>
        /// Returns a service object given the specified <see cref="T:System.ServiceModel.InstanceContext" /> object.
        /// </summary>
        /// <param name="instanceContext">The current <see cref="T:System.ServiceModel.InstanceContext" /> object.</param>
        /// <returns>
        /// A user-defined service object.
        /// </returns>
        public object GetInstance(InstanceContext instanceContext)
        {
            return GetInstance(instanceContext, null);
        }

        /// <summary>
        /// Called when an <see cref="T:System.ServiceModel.InstanceContext" /> object recycles a service object.
        /// </summary>
        /// <param name="instanceContext">The service's instance context.</param>
        /// <param name="instance">The service object to be recycled.</param>
        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            //instanceContext.Extensions.Find<ServiceInstanceContextExtension>().DisposeChildScope();
        }
    }
}
