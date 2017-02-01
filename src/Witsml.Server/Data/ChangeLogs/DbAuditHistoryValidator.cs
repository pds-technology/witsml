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
using PDS.Framework;
using PDS.Witsml.Data.ChangeLogs;

namespace PDS.Witsml.Server.Data.ChangeLogs
{
    /// <summary>
    /// Provides validation for <see cref="DbAuditHistory" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{DbAuditHistory}" />
    [Export(typeof(IDataObjectValidator<DbAuditHistory>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class DbAuditHistoryValidator : DataObjectValidator<DbAuditHistory>
    {
        private readonly IWitsmlDataAdapter<Well> _wellDataAdapter;
        private readonly IWitsmlDataAdapter<Wellbore> _wellboreDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbAuditHistoryValidator" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        [ImportingConstructor]
        public DbAuditHistoryValidator(
            IContainer container,
            IWitsmlDataAdapter<Well> wellDataAdapter,
            IWitsmlDataAdapter<Wellbore> wellboreDataAdapter)
            : base(container)
        {
            _wellDataAdapter = wellDataAdapter;
            _wellboreDataAdapter = wellboreDataAdapter;
        }
    }
}