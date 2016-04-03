//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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

using System.Security;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Client.Linq
{
    public class Witsml141Context : WitsmlContext
    {
        public Witsml141Context(string url, double timeoutInMinutes = 1.5)
            : base(url, timeoutInMinutes, WMLSVersion.WITSML141)
        {
        }

        public Witsml141Context(string url, string username, string password, double timeoutInMinutes = 1.5)
            : base(url, username, password, timeoutInMinutes, WMLSVersion.WITSML141)
        {
        }

        public Witsml141Context(string url, string username, SecureString password, double timeoutInMinutes = 1.5)
            : base(url, username, password, timeoutInMinutes, WMLSVersion.WITSML141)
        {
        }

        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version141.Value; }
        }

        public IWitsmlQuery<Well> Wells
        {
            get { return CreateQuery<Well, WellList>(); }
        }

        public IWitsmlQuery<Wellbore> Wellbores
        {
            get { return CreateQuery<Wellbore, WellboreList>(); }
        }

        public IWitsmlQuery<Rig> Rigs
        {
            get { return CreateQuery<Rig, RigList>(); }
        }

        public IWitsmlQuery<Log> Logs
        {
            get { return CreateQuery<Log, LogList>(); }
        }

        public IWitsmlQuery<Trajectory> Trajectories
        {
            get { return CreateQuery<Trajectory, TrajectoryList>(); }
        }
    }
}
