//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2017.2
//
// Copyright 2017 PDS Americas LLC
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

using System.ServiceModel;
using System.Web.Http.Dependencies;

namespace PDS.WITSMLstudio.Framework.Web.Services
{
    /// <summary>
    /// Service extension that provides access to the composition container used for dependency injection.
    /// </summary>
    public class ServiceInstanceContextExtension : IExtension<InstanceContext>
    {
        private IDependencyScope _scope;

        /// <summary>
        /// Enables an extension object to find out when it has been aggregated. Called when the extension is 
        /// added to the <see cref="P:System.ServiceModel.IExtensibleObject`1.Extensions" /> property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Attach(InstanceContext owner)
        {
        }

        /// <summary>
        /// Enables an object to find out when it is no longer aggregated. Called when an extension is 
        /// removed from the <see cref="P:System.ServiceModel.IExtensibleObject`1.Extensions" /> property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Detach(InstanceContext owner)
        {
        }

        /// <summary>
        /// Gets the child scope.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <returns>The child scope.</returns>
        public IDependencyScope GetChildScope(IDependencyResolver resolver)
        {
            if (_scope == null)
            {
                _scope = resolver.BeginScope();
            }

            return _scope;
        }

        /// <summary>
        /// Disposes the child scope.
        /// </summary>
        public void DisposeChildScope()
        {
            if (_scope != null)
            {
                _scope.Dispose();
            }
        }
    }
}
