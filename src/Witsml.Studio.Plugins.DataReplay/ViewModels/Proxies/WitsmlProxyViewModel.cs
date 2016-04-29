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

using System.Threading;
using System.Threading.Tasks;
using Energistics.DataAccess;
using PDS.Witsml.Studio.Core.Connections;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Proxies
{
    public abstract class WitsmlProxyViewModel
    {
        public WitsmlProxyViewModel(Connection connection, WMLSVersion version)
        {
            Connection = connection.CreateProxy(version);
            Version = version;
        }

        public WITSMLWebServiceConnection Connection { get; private set; }

        public WMLSVersion Version { get; private set; }

        public abstract Task Start(Models.Simulation model, CancellationToken token, int interval = 5000);
    }
}
