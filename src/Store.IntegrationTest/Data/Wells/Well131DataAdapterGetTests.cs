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

using System;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    /// <summary>
    /// Well131DataAdapterGetTests
    /// </summary>
    [TestClass]
    public partial class Well131DataAdapterGetTests : Well131TestBase
    {
        [TestMethod]
        public void Well131DataAdapter_GetFromStore_Selection_Not_Equal_Comparison_dTimLastChange()
        {
            var well01 = DevKit.GetFullWell();
            well01.Uid = DevKit.Uid();
            var response = DevKit.Add<WellList, Well>(well01);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid01 = response.SuppMsgOut;

            var query = new Well { Uid = uid01 };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(uid01, result[0].Uid);

            var wellLastChangeTime = result[0].CommonData.DateTimeLastChange;

            var well02 = DevKit.GetFullWell();
            well02.Uid = DevKit.Uid();
            well02.CommonData.DateTimeCreation = DateTimeOffset.UtcNow;
            response = DevKit.Add<WellList, Well>(well02);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid02 = response.SuppMsgOut;

            query = new Well { CommonData = new CommonData() };
            query.CommonData.DateTimeLastChange = wellLastChangeTime;
            result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 6.6.4
            Assert.IsTrue(result.Any(x => x.Uid == uid02));
            Assert.IsFalse(result.Any(x => x.Uid == uid01));
        }

        [TestMethod]
        public void Well131DataAdapter_GetFromStore_Parse_DocumentInfo_Element()
        {
            var queryInWithDocumentInfo =
                    @"<wells xmlns=""http://www.witsml.org/schemas/131"" xmlns:xlink=""http://www.w3.org/1999/xlink"" 
                            xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:dc=""http://purl.org/dc/terms/"" 
                            xmlns:gml=""http://www.opengis.net/gml/3.2"" version=""1.3.1.1"" >                      
                        <documentInfo />
                        <well>
                            <name />
                        </well>
                    </wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, queryInWithDocumentInfo, null, "");
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellListObject = EnergisticsConverter.XmlToObject<WellList>(response.XMLout);
            Assert.IsNotNull(wellListObject);
            Assert.IsNotNull(wellListObject.DocumentInfo);
            Assert.AreEqual(ObjectTypes.Well, wellListObject.DocumentInfo.DocumentName.Value);
        }

        [TestMethod]
        public void Well131DataAdapter_GetFromStore_Ignore_Uom_Attributes()
        {
            var well = DevKit.CreateTestWell();
            var responseAddWell = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(responseAddWell);
            Assert.AreEqual((short)ErrorCodes.Success, responseAddWell.Result);

            var uid = responseAddWell.SuppMsgOut;

            string queryIn = @" <wells xmlns=""http://www.witsml.org/schemas/131"" version=""1.3.1.1"">                                
                                <well uid=""" + uid + @""">
                                    <name />
                                    <groundElevation uom=""m"" />
                                    <measuredDepth uom=""ft"" /> 
                                    <waterDepth uom=""ft"" />                               
                                </well>
                                </wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, queryIn, null, "");
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellListObject = EnergisticsConverter.XmlToObject<WellList>(response.XMLout);
            Assert.IsNotNull(wellListObject);
            Assert.AreEqual(1, wellListObject.Well.Count);
            Assert.AreEqual(uid, wellListObject.Well[0].Uid);
            Assert.AreEqual(well.Name, wellListObject.Well[0].Name);
        }
    }
}
