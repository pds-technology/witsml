//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
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
using System.Threading.Tasks;
using Avro.Specific;
using Energistics.Common;
using Energistics.IntegrationTest;
using Energistics.Security;

namespace Energistics
{
    /// <summary>
    /// Common base class for all ETP DevKit integration tests.
    /// </summary>
    public abstract class IntegrationTestBase
    {
        protected static readonly string AuthTokenUrl = Settings.Default.AuthTokenUrl;
        protected static readonly string ServerUrl = Settings.Default.ServerUrl;
        protected static readonly string Username = Settings.Default.Username;
        protected static readonly string Password = Settings.Default.Password;

        /// <summary>
        /// Creates an <see cref="EtpClient"/> instance configurated with the
        /// current connection and authorization parameters.
        /// </summary>
        /// <returns></returns>
        protected EtpClient CreateClient()
        {
            var version = GetType().Assembly.GetName().Version.ToString();
            var headers = Authorization.Basic(Username, Password);

            return new EtpClient(ServerUrl, GetType().AssemblyQualifiedName, version, headers);
        }

        /// <summary>
        /// Handles an event asynchronously and waits for it to complete.
        /// </summary>
        /// <typeparam name="T">The type of ETP message.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <returns>An awaitable task.</returns>
        protected async Task<ProtocolEventArgs<T>> HandleAsync<T>(Action<ProtocolEventHandler<T>> action)
            where T : ISpecificRecord
        {
            ProtocolEventArgs<T> args = null;
            var task = new Task<ProtocolEventArgs<T>>(() => args);

            action((s, e) =>
            {
                args = e;
                task.Start();
            });

            return await task.WaitAsync();
        }

        /// <summary>
        /// Handles an event asynchronously and waits for it to complete.
        /// </summary>
        /// <typeparam name="T">The type of ETP message.</typeparam>
        /// <typeparam name="TContext">The type of the context.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <returns>An awaitable task.</returns>
        protected async Task<ProtocolEventArgs<T, TContext>> HandleAsync<T, TContext>(Action<ProtocolEventHandler<T, TContext>> action)
            where T : ISpecificRecord
        {
            ProtocolEventArgs<T, TContext> args = null;
            var task = new Task<ProtocolEventArgs<T, TContext>>(() => args);

            action((s, e) =>
            {
                args = e;
                task.Start();
            });

            return await task.WaitAsync();
        }
    }
}
