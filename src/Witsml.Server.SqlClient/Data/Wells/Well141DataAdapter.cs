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
using Energistics.DataAccess.WITSML141;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Wells
{
    [Export(typeof(IWitsmlDataAdapter<Well>))]
    [Export(typeof(IWitsml141Configuration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well141DataAdapter : SqlWitsmlDataAdapter<Well>, IWitsml141Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Well141DataAdapter" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Well141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Well141)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Well"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            if (!DatabaseProvider.SchemaMapper.IsAvailable(ObjectName))
                return;

            capServer.Add(Functions.GetFromStore, ObjectTypes.Well);
            //capServer.Add(Functions.AddToStore, ObjectTypes.Well);
            //capServer.Add(Functions.UpdateInStore, ObjectTypes.Well);
            //capServer.Add(Functions.DeleteFromStore, ObjectTypes.Well);
        }

        /// <summary>
        /// Creates the query template.
        /// </summary>
        /// <returns>A query template.</returns>
        protected override WitsmlQueryTemplate<Well> CreateQueryTemplate()
        {
            return new WitsmlQueryTemplate<Well>(
                new Well()
                {
                    Uid = "abc",
                    Name = "abc"
                });
        }
    }
}
