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
using Energistics.DataAccess.WITSML200;
using PDS.Framework;


namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Data provider that implements support for WITSML API functions for <see cref="Well"/>.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.EtpDataProvider{Well}" />
    [Export(typeof(IEtpDataProvider))]
    [Export(typeof(IEtpDataProvider<Well>))]
    [Export200(ObjectTypes.Well, typeof(IEtpDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class Well200DataProvider : EtpDataProvider<Well>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Well200DataProvider"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        [ImportingConstructor]
        public Well200DataProvider(IContainer container, IWitsmlDataAdapter<Well> dataAdapter) : base(container, dataAdapter)
        {
        }
    }
}