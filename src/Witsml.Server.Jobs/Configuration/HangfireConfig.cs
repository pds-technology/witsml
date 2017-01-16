//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
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
using Hangfire;
using PDS.Framework;

namespace PDS.Witsml.Server.Jobs.Configuration
{
    /// <summary>
    /// Configures Hangfire jobs for the Witsml Server.
    /// </summary>
    public static class HangfireConfig
    {
        /// <summary>
        /// Registers Hangfire jobs for the specified container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>An IDisposible reference to the Hangfire BackgroundJobServer.</returns>
        public static IDisposable Register(IContainer container)
        {
            GlobalConfiguration.Configuration.UseActivator(new ContainerJobActivator(container));
            var backgroundJobServer = new BackgroundJobServer();

            BackgroundJob.Enqueue<ObjectGrowingManager>(x => x.Start());
            return backgroundJobServer;
        }
    }
}
