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

using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace PDS.WITSMLstudio.Framework.Web.Services
{
    /// <summary>
    /// Service extension initializer implementation.
    /// </summary>
    /// <seealso cref="System.ServiceModel.Dispatcher.IInstanceContextInitializer" />
    public class ServiceInstanceContextInitializer : IInstanceContextInitializer
    {
        /// <summary>
        /// Provides the ability to modify the newly created <see cref="T:System.ServiceModel.InstanceContext" /> object.
        /// </summary>
        /// <param name="instanceContext">The system-supplied instance context.</param>
        /// <param name="message">The message that triggered the creation of the instance context.</param>
        public void Initialize(InstanceContext instanceContext, Message message)
        {
            instanceContext.Extensions.Add(new ServiceInstanceContextExtension());
        }
    }
}
