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

namespace Energistics
{
    public class IntegrationTestBase
    {
        public static readonly string ServerUrl = Settings.Default.ServerUrl;
        public static readonly string Username = Settings.Default.Username;
        public static readonly string Password = Settings.Default.Password;

        public EtpClient CreateClient()
        {
            var version = GetType().Assembly.GetName().Version.ToString();
            var headers = EtpClient.Authorization(Username, Password);

            return new EtpClient(ServerUrl, GetType().AssemblyQualifiedName, version, headers);
        }

        protected async Task<ProtocolEventArgs<T>> HandleAsync<T>(Action<ProtocolEventHandler<T>> action) where T : ISpecificRecord
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

        protected async Task<ProtocolEventArgs<T, V>> HandleAsync<T, V>(Action<ProtocolEventHandler<T, V>> action) where T : ISpecificRecord
        {
            ProtocolEventArgs<T, V> args = null;
            var task = new Task<ProtocolEventArgs<T, V>>(() => args);

            action((s, e) =>
            {
                args = e;
                task.Start();
            });

            return await task.WaitAsync();
        }
    }
}
