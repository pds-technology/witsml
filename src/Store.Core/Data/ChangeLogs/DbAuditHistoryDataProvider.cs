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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.ChangeLogs;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.ChangeLogs
{
    /// <summary>
    /// Data provider that implements support for WITSML API functions for <see cref="DbAuditHistory" />.
    /// </summary>
    /// <seealso cref="ChangeLogList" />
    [Export141(ObjectTypes.ChangeLog, typeof(IWitsmlDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DbAuditHistoryDataProvider : WitsmlDataProvider<DbAuditHistoryList, DbAuditHistory>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbAuditHistoryDataProvider"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        [ImportingConstructor]
        public DbAuditHistoryDataProvider(IContainer container, IWitsmlDataAdapter<DbAuditHistory> dataAdapter) : base(container, dataAdapter)
        {
        }

        /// <summary>
        /// Gets object(s) from store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>Queried objects.</returns>
        public override WitsmlResult<IEnergisticsCollection> GetFromStore(RequestContext context)
        {
            var result = base.GetFromStore(context);
            var list = (DbAuditHistoryList)result.Results;

            list.ChangeLog = list.ChangeLog
                .Cast<DbAuditHistory>()
                .Select(c => c.ToChangeLog())
                .ToList();

            return result;
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected override void SetDefaultValues(DbAuditHistory dataObject)
        {
            dataObject.Uid = dataObject.NewUid();
            dataObject.CommonData = dataObject.CommonData.Create();
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="uri">The data object URI.</param>
        protected override void SetDefaultValues(DbAuditHistory dataObject, EtpUri uri)
        {
            dataObject.Uid = uri.ObjectId;
            dataObject.Name = dataObject.Uid;
        }

        /// <summary>
        /// Creates a new <see cref="ChangeLogList" /> instance containing the specified data objects.
        /// </summary>
        /// <param name="dataObjects">The data objects.</param>
        /// <returns>A new <see cref="ChangeLogList" /> instance.</returns>
        protected override DbAuditHistoryList CreateCollection(List<DbAuditHistory> dataObjects)
        {
            return new DbAuditHistoryList { ChangeLog = dataObjects.Cast<ChangeLog>().ToList() };
        }
    }
}
