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
using System.Collections.Concurrent;
using Hangfire;
using PDS.Framework;

namespace PDS.Witsml.Server.Jobs.Configuration
{
    /// <summary>
    /// Configures Hangfire jobs for the Witsml Server.
    /// </summary>
    public static class HangfireConfig
    {
        private static IDisposable _backgroundJobServer;
        private static ConcurrentStack<string> _jobIds;

        /// <summary>
        /// Registers Hangfire jobs for the specified container.
        /// </summary>
        /// <param name="container">The composition container.</param>
        public static void Register(IContainer container)
        {
            GlobalConfiguration.Configuration.UseActivator(new ContainerJobActivator(container));
            _backgroundJobServer = new BackgroundJobServer();
            _jobIds = new ConcurrentStack<string>();

            var jobId = BackgroundJob.Enqueue<ObjectGrowingManager>(x => x.Start());
            _jobIds.Push(jobId);
        }

        /// <summary>
        /// Deletes background jobs and disposes Hangfire server.
        /// </summary>
        public static void Unregister()
        {
            _jobIds.ForEach(x => BackgroundJob.Delete(x));
            _backgroundJobServer.Dispose();
            _jobIds.Clear();
        }
    }
}
