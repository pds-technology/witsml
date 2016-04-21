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

using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML131;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Wells
{
    [Export(typeof(IEtpDataProvider))]
    [Export(typeof(IWitsml131Configuration))]
    [Export131(ObjectTypes.Well, typeof(IEtpDataProvider))]
    [Export131(ObjectTypes.Well, typeof(IWitsmlDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well131DataProvider : WitsmlDataProvider<WellList, Well>, IWitsml131Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Well131DataProvider"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        [ImportingConstructor]
        public Well131DataProvider(IContainer container, IWitsmlDataAdapter<Well> dataAdapter) : base(container, dataAdapter)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Well"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            Logger.DebugFormat("Getting capabilities for server '{0}'.", capServer.Name);

            capServer.Add(Functions.GetFromStore, ObjectTypes.Well);
            capServer.Add(Functions.AddToStore, ObjectTypes.Well);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Well);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Well);
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected override void SetDefaultValues(Well dataObject)
        {
            dataObject.Uid = dataObject.NewUid();
            dataObject.CommonData = dataObject.CommonData.Create();
        }
    }
}
