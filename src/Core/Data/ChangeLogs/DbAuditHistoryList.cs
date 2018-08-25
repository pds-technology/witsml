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
using Energistics.DataAccess.WITSML141;

namespace PDS.WITSMLstudio.Data.ChangeLogs
{
    /// <summary>
    /// Extended version of 141 ChangeLogList for use as a common version ChangeLogList
    /// </summary>
    /// <seealso cref="Energistics.DataAccess.WITSML141.ChangeLogList" />
    [XmlInclude(typeof(ChangeLog))]
    [XmlInclude(typeof(DbAuditHistory))]
    [XmlType(TypeName = "changeLogs", Namespace = "http://www.witsml.org/schemas/1series")]
    [XmlRoot("changeLogs", IsNullable = false, Namespace = "http://www.witsml.org/schemas/1series")]
    public class DbAuditHistoryList : ChangeLogList
    {
    }
}
