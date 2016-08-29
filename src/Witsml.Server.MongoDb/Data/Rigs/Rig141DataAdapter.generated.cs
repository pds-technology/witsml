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


namespace PDS.Witsml.Server.Data.Rigs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Rig" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Rig}" />
    [Export(typeof(IWitsmlDataAdapter<Rig>))]
    [Export(typeof(IWitsml141Configuration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class Rig141DataAdapter : MongoDbDataAdapter<Rig>, IWitsml141Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rig141DataAdapter" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Rig141DataAdapter(IContainer container, IDatabaseProvider databaseProvider)
            : base(container, databaseProvider, ObjectNames.Rig141)
        {
            Logger.Debug("Instance created.");
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Rig"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            Logger.DebugFormat("Getting the supported capabilities for Rig data version {0}.", capServer.Version);

            capServer.Add(Functions.GetFromStore, ObjectTypes.Rig);
            capServer.Add(Functions.AddToStore, ObjectTypes.Rig);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Rig);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Rig);
        }
    }
}
