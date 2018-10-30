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

using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Attachments
{
    /// <summary>
    /// Attachment141DataAdapterUpdateTests
    /// </summary>
    [TestClass]
    public partial class Attachment141DataAdapterUpdateTests : Attachment141TestBase
    {
        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            Attachment.FileName = "image.png";
            Attachment.FileType = "image/png";
        }

        [TestMethod]
        public void Attachment141DataAdapter_UpdateInStore_Can_Add_And_Update_Attachment()
        {
            AddParents();

            DevKit.AddAndAssert<AttachmentList, Attachment>(Attachment);
            DevKit.UpdateAndAssert<AttachmentList, Attachment>(Attachment);
            var result = DevKit.GetAndAssert<AttachmentList, Attachment>(Attachment);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Content);
            Assert.IsTrue(Attachment.Content.SequenceEqual(result.Content));
        }
    }
}
