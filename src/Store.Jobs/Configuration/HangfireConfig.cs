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
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Jobs.Configuration
{
    /// <summary>
    /// Configures Hangfire jobs for the Witsml Server.
    /// </summary>
    public static class HangfireConfig
    {
        private static IDisposable _backgroundJobServer;

        /// <summary>
        /// Registers Hangfire jobs for the specified container.
        /// </summary>
        /// <param name="container">The composition container.</param>
        public static void Register(IContainer container)
        {
            GlobalConfiguration.Configuration
                .UseActivator(new ContainerJobActivator(container))
                .UseLog4NetLogProvider();

            _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                HeartbeatInterval = TimeSpan.FromDays(1),
                ServerCheckInterval = TimeSpan.FromDays(1)
            });

            var changeDetectionPeriod = Math.Max(WitsmlSettings.ChangeDetectionPeriod / 60, 1);
            var cronExpression = $"*/{changeDetectionPeriod} * * * *";

            RecurringJob.AddOrUpdate<ObjectGrowingManager>(ObjectGrowingManager.JobId, x => x.Start(), cronExpression);
        }

        /// <summary>
        /// Deletes background jobs and disposes Hangfire server.
        /// </summary>
        public static void Unregister()
        {
            _backgroundJobServer?.Dispose();
        }
    }
}
