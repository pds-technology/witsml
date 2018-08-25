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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.MudLogs
{
    [TestClass]
    public partial class MudLog141DataAdapterAddTests : MudLog141TestBase
    {
        [TestMethod]
        public void MudLog141DataAdapter_AddToStore_Can_Add_Detailed_MudLog()
        {
            AddParents();
            MudLog.GeologyInterval = DevKit.MudLogGenerator.GenerateGeologyIntervals(5, 10.0, includeLithology: true, includeChromatograph: true, includeShow: true);
            DevKit.AddAndAssert(MudLog);

            TestReset(5);

            AddParents();
            MudLog.GeologyInterval = DevKit.MudLogGenerator.GenerateGeologyIntervals(10, 10.0, includeLithology: true, includeChromatograph: true, includeShow: true);
            DevKit.AddAndAssert(MudLog);
        }
    }
}
