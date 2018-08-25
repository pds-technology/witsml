//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML141;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.ChangeLogs;

namespace PDS.WITSMLstudio.Store.Data.ChangeLogs
{
    /// <summary>
    /// Provides validation for <see cref="DbAuditHistory" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.DataObjectValidator{DbAuditHistory}" />
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
