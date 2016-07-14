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

using System;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well141MongoDataAdapterValidationTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _devKit = null;
        }

        [TestMethod]
        public void MongoDbUpdate_UpdateInStore_Error_445_Empty_New_Element()
        {
            AddWell();

            var update = new Well
            {
                Uid = _well.Uid,
                WellPublicLandSurveySystemLocation = new PublicLandSurveySystem
                {
                    QuarterTownship = string.Empty,
                    Township = 1
                }
            };

            var response = _devKit.Update<WellList, Well>(update);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, response.Result);
        }

        [TestMethod]
        public void MongoDbUpdate_UpdateInStore_Error_445_Empty_New_Attribute()
        {
            AddWell();

            var update = new Well
            {
                Uid = _well.Uid,
                WellheadElevation = new WellElevationCoord
                {
                    Uom = WellVerticalCoordinateUom.m,
                    Value = 1,
                    Datum = string.Empty
                }
            };

            var response = _devKit.Update<WellList, Well>(update);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, response.Result);
        }

        [TestMethod, Description("When adding a new element has nested uom and value, uom should not be specified if there is no value")]
        public void MongoDbUpdate_UpdateInStore_Error_446_Uom_Exist_Without_Value_Nested_Element()
        {
            AddWell();

            var updateXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<well uid=\"" + _well.Uid + "\">" + Environment.NewLine +
                "<wellheadElevation uom=\"m\" datum=\"KB\"></wellheadElevation>" + Environment.NewLine +
                "</well>" + Environment.NewLine +
                "</wells>";

            var response = _devKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, response.Result);
        }

        [TestMethod, Description("When adding a new recurring element has nested uom and value, uom should not be specified if there is no value")]
        public void MongoDbUpdate_UpdateInStore_Error_446_Uom_Exist_Without_Value_Array_Element()
        {
            AddWell();

            var updateXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<well uid=\"" + _well.Uid + "\">" + Environment.NewLine +
                    "<wellDatum uid=\"KB\" >" + Environment.NewLine +
                        "<name>Kelly Bushing</name>" + Environment.NewLine +
                        "<code>KB</code>" + Environment.NewLine +
                        "<elevation uom=\"ft\"/>" + Environment.NewLine +
                    "</wellDatum>" + Environment.NewLine +
                    "<wellDatum uid=\"DF\" >" + Environment.NewLine +
                        "<name>Derrick Floor</name>" + Environment.NewLine +
                        "<code>DF</code>" + Environment.NewLine +
                    "</wellDatum>" + Environment.NewLine +
                "</well>" + Environment.NewLine +
                "</wells>";

            var response = _devKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, response.Result);
        }

        private void AddWell()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
