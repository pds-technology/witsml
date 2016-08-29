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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.Datatypes;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;


namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Wellbore" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Wellbore}" />
    [Export(typeof(IWitsmlDataAdapter<Wellbore>))]
    [Export(typeof(IWitsml141Configuration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class Wellbore141DataAdapter : MongoDbDataAdapter<Wellbore>, IWitsml141Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Wellbore141DataAdapter" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Wellbore141DataAdapter(IContainer container, IDatabaseProvider databaseProvider)
            : base(container, databaseProvider, ObjectNames.Wellbore141)
        {
            Logger.Debug("Instance created.");
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Wellbore"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            Logger.DebugFormat("Getting the supported capabilities for Wellbore data version {0}.", capServer.Version);

            capServer.Add(Functions.GetFromStore, ObjectTypes.Wellbore);
            capServer.Add(Functions.AddToStore, ObjectTypes.Wellbore);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Wellbore);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Wellbore);
        }
    }
}
