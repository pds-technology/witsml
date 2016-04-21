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
using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ReferenceData;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Logs
{
    [Export(typeof(IEtpDataProvider))]
    [Export(typeof(IWitsml131Configuration))]
    [Export141(ObjectTypes.Log, typeof(IEtpDataProvider))]
    [Export131(ObjectTypes.Log, typeof(IWitsmlDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log131DataProvider : WitsmlDataProvider<LogList, Log>, IWitsml131Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Log131DataProvider"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        [ImportingConstructor]
        public Log131DataProvider(IContainer container, IWitsmlDataAdapter<Log> dataAdapter) : base(container, dataAdapter)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Log"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            capServer.Add(Functions.GetFromStore, ObjectTypes.Log);
            capServer.Add(Functions.AddToStore, ObjectTypes.Log);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Log);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Log);
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected override void SetDefaultValues(Log dataObject)
        {
            dataObject.Uid = dataObject.NewUid();
            dataObject.CommonData = dataObject.CommonData.Create();

            // Ensure Direction
            if (!dataObject.Direction.HasValue)
            {
                dataObject.Direction = LogIndexDirection.increasing;
            }

            if (dataObject.LogCurveInfo != null)
            {
                // Ensure UID
                dataObject.LogCurveInfo
                    .Where(x => string.IsNullOrWhiteSpace(x.Uid))
                    .ForEach(x => x.Uid = x.Mnemonic);

                // Ensure index curve is first
                dataObject.LogCurveInfo.MoveToFirst(dataObject.IndexCurve.Value);
            }
        }
    }
}
