//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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
using Hangfire;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Jobs.Configuration
{
    /// <summary>
    /// Custom class to override the Hangfire JobActivator to use the PDS.WITSMLstudio.Framework IoC container.
    /// </summary>
    /// <seealso cref="Hangfire.JobActivator" />
    public class ContainerJobActivator : JobActivator
    {
        private readonly IContainer _container;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerJobActivator"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public ContainerJobActivator(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Activates the job.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The resolved job type.</returns>
        public override object ActivateJob(Type type)
        {
            return _container.Resolve(type);
        }
    }
}
