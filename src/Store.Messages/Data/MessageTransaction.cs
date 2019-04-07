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

using System.ComponentModel.Composition;
using PDS.WITSMLstudio.Store.Transactions;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Placeholder for a message transaction.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Transactions.WitsmlTransaction" />
    [Export(typeof(IWitsmlTransaction))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MessageTransaction : WitsmlTransaction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTransaction"/> class.
        /// </summary>
        [ImportingConstructor]
        public MessageTransaction()
        {
            InitializeRootTransaction();
        }
    }
}