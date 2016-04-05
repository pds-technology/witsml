//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
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
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Energistics.DataAccess;

namespace PDS.Witsml.Studio.Connections
{
    /// <summary>
    /// Provides a connection test for a Witsml Connection instance.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Studio.Connections.IConnectionTest" />
    [Export("Witsml", typeof(IConnectionTest))]
    public class WitsmlConnectionTest : IConnectionTest
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(WitsmlConnectionTest));

        /// <summary>
        /// Determines whether this Connection instance can connect to the specified connection Uri.
        /// </summary>
        /// <param name="connection">The connection instanace being tested.</param>
        /// <returns>The boolean result from the asynchronous operation.</returns>
        public async Task<bool> CanConnect(Connection connection)
        {
            try
            {
                var proxy = new WITSMLWebServiceConnection(connection.Uri, WMLSVersion.WITSML141);

                if (!string.IsNullOrWhiteSpace(connection.Username))
                {
                    proxy.Username = connection.Username;
                    proxy.SetSecurePassword(connection.SecurePassword);
                }

                var versions = proxy.GetVersion();

                _log.Debug("Witsml connection test passed");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _log.Debug("Witsml connection test failed: {0}", ex);
                return await Task.FromResult(false);
            }
        }
    }
}
