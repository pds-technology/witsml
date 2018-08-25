//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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

using System.Xml.Serialization;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;

namespace PDS.WITSMLstudio.Data.ChangeLogs
{
    /// <summary>
    /// Extended version of 141 ChangeLog for use as a common version ChangeLog
    /// </summary>
    /// <seealso cref="ChangeLog" />
    [XmlType(TypeName = "changeLog", Namespace = "http://www.witsml.org/schemas/1series")]
    public class DbAuditHistory : ChangeLog, IDataObject
    {
        /// <summary>
        /// Gets or sets the URI for the changeLog.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the changeLog name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// To the change log.
        /// </summary>
        /// <returns></returns>
        public ChangeLog ToChangeLog()
        {
            return new ChangeLog
            {
                NameWell = NameWell,
                NameWellbore = NameWellbore,
                NameObject = NameObject,
                ObjectType = ObjectType,
                SourceName = SourceName,
                LastChangeType = LastChangeType,
                LastChangeInfo = LastChangeInfo,
                ChangeHistory = ChangeHistory,
                CommonData = CommonData,
                CustomData = CustomData,
                UidWell = UidWell,
                UidWellbore = UidWellbore,
                UidObject = UidObject,
                Uid = Uid
            };
        }
    }
}
