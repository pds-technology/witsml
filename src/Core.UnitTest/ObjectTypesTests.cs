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

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Energistics.DataAccess;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace PDS.WITSMLstudio
{
    [TestClass]
    public class ObjectTypesTests
    {
        private static readonly string _wellsXml =
            "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + Environment.NewLine +
            "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
            "  <documentInfo />" + Environment.NewLine +
            "  <well />" + Environment.NewLine +
            "</wells>";

        [TestMethod]
        public void ObjectTypes_GetObjectType_Returns_Valid_Witsml_Type_For_Well()
        {
            const string expected = ObjectTypes.Well;

            Assert.AreEqual(expected, ObjectTypes.GetObjectType<Witsml141.WellList>());
            Assert.AreEqual(expected, ObjectTypes.GetObjectType(new Witsml141.WellList()));
            Assert.AreEqual(expected, ObjectTypes.GetObjectType(new Witsml141.Well()));
            Assert.AreEqual(expected, ObjectTypes.GetObjectType(new Witsml200.Well()));
        }

        [TestMethod]
        public void ObjectTypes_GetObjectType_Returns_Valid_Witsml_Type_For_Wellbore()
        {
            const string expected = ObjectTypes.Wellbore;

            Assert.AreEqual(expected, ObjectTypes.GetObjectType(typeof(Witsml141.WellboreList)));
            Assert.AreEqual(expected, ObjectTypes.GetObjectType(typeof(Witsml141.Wellbore)));
            Assert.AreEqual(expected, ObjectTypes.GetObjectType(typeof(Witsml200.Wellbore)));
        }

        [TestMethod]
        public void ObjectTypes_GetObjectType_Returns_Null_For_Invalid_Witsml_Type()
        {
            Should.Throw<ArgumentException>(() =>
            {
                ObjectTypes.GetObjectType(typeof(DateTime));
            });
        }

        [TestMethod]
        public void ObjectTypes_GetObjectType_Returns_Correct_WbGeometry_Type_For_131()
        {
            var objectType = ObjectTypes.GetObjectType(ObjectTypes.WbGeometry, ObjectFamilies.Witsml, WMLSVersion.WITSML131);
            Assert.AreEqual(typeof(Witsml131.StandAloneWellboreGeometry), objectType);

            objectType = ObjectTypes.GetObjectType(ObjectTypes.WellboreGeometry, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version131.Value);
            Assert.IsNull(objectType);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectType_Returns_Correct_WbGeometry_Type_For_141()
        {
            var objectType = ObjectTypes.GetObjectType(ObjectTypes.WbGeometry, ObjectFamilies.Witsml, WMLSVersion.WITSML141);
            Assert.AreEqual(typeof(Witsml141.StandAloneWellboreGeometry), objectType);

            objectType = ObjectTypes.GetObjectType(ObjectTypes.WellboreGeometry, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version141.Value);
            Assert.IsNull(objectType);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectType_Returns_Correct_WbGeometry_Type_For_200()
        {
            var objectType = ObjectTypes.GetObjectType(ObjectTypes.WbGeometry, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version200.Value);
            Assert.IsNull(objectType);

            objectType = ObjectTypes.GetObjectType(ObjectTypes.WellboreGeometry, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version200.Value);
            Assert.AreEqual(typeof(Witsml200.WellboreGeometry), objectType);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectType_Returns_Type_For_Valid_Xml()
        {
            var document = WitsmlParser.Parse(_wellsXml);
            var typeFound = ObjectTypes.GetObjectType(document.Root);
            Assert.AreEqual(ObjectTypes.Well, typeFound);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectType_Returns_Unknown_For_Invalid_Xml()
        {
            var typeFound = ObjectTypes.GetObjectType((XElement)null);
            Assert.AreEqual(ObjectTypes.Unknown, typeFound);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectTypeFromGroup_Returns_Type_For_Valid_Xml()
        {
            var document = WitsmlParser.Parse(_wellsXml);
            var typeFound = ObjectTypes.GetObjectTypeFromGroup(document.Root);
            Assert.AreEqual(ObjectTypes.Well, typeFound);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectTypeFromGroup_Returns_Unknown_For_Invalid_Xml()
        {
            var typeFound = ObjectTypes.GetObjectTypeFromGroup(null);
            Assert.AreEqual(ObjectTypes.Unknown, typeFound);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectTypeListProperty_Returns_Property_Name_For_WellList()
        {
            var propertyName = ObjectTypes.GetObjectTypeListProperty(ObjectTypes.Well, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version141);
            Assert.AreEqual("Well", propertyName);

            Assert.IsNull(ObjectTypes.GetObjectTypeListProperty(ObjectTypes.Well, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version200.Value));
        }

        [TestMethod]
        public void ObjectTypes_GetObjectTypeListPropertyInfo_Returns_PropertyInfo_For_WellList()
        {
            var property = ObjectTypes.GetObjectTypeListPropertyInfo(ObjectTypes.Well, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version141);
            Assert.AreEqual("Well", property.Name);
            Assert.AreEqual(typeof(List<Witsml141.Well>), property.PropertyType);

            Assert.IsNull(ObjectTypes.GetObjectTypeListPropertyInfo(ObjectTypes.Well, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version200.Value));
        }

        [TestMethod]
        public void ObjectTypes_GetObjectGroupType_Returns_Correct_WbGeometry_Type_For_131()
        {
            var objectType = ObjectTypes.GetObjectGroupType(ObjectTypes.WbGeometry, ObjectFamilies.Witsml, WMLSVersion.WITSML131);
            Assert.AreEqual(typeof(Witsml131.WellboreGeometryList), objectType);
            Assert.AreEqual(ObjectTypes.Unknown, ObjectTypes.GetObjectGroupType(null));
        }

        [TestMethod]
        public void ObjectTypes_GetObjectGroupType_Returns_Correct_WbGeometry_Type_For_141()
        {
            var objectType = ObjectTypes.GetObjectGroupType(ObjectTypes.WbGeometry, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version141);
            Assert.AreEqual(typeof(Witsml141.WellboreGeometryList), objectType);
        }

        [TestMethod]
        public void ObjectTypes_GetSchemaType_Returns_Data_Object_Xsd_Type_Name()
        {
            Assert.AreEqual("obj_well", ObjectTypes.GetSchemaType(new Witsml141.Well()));
            Assert.AreEqual("Well", ObjectTypes.GetSchemaType(typeof(Witsml200.Well)));
            Assert.IsNull(ObjectTypes.GetSchemaType(null));
        }

        [TestMethod]
        public void ObjectTypes_GetVersion_Returns_Version_From_Valid_Xml()
        {
            var document = WitsmlParser.Parse(_wellsXml);
            var version = ObjectTypes.GetVersion(document.Root);
            Assert.AreEqual(OptionsIn.DataVersion.Version141.Value, version);
        }

        [TestMethod]
        public void ObjectTypes_GetVersion_Returns_Empty_Version_From_Invalid_Xml()
        {
            var version = ObjectTypes.GetVersion((XElement)null);
            Assert.AreEqual(string.Empty, version);
        }

        [TestMethod]
        public void ObjectTypes_GetVersion_Returns_Version_From_Type()
        {
            Assert.AreEqual(OptionsIn.DataVersion.Version131.Value, ObjectTypes.GetVersion(typeof(Witsml131.WellList)));
            Assert.AreEqual(OptionsIn.DataVersion.Version141.Value, ObjectTypes.GetVersion(typeof(Witsml141.Well)));
            Assert.AreEqual(OptionsIn.DataVersion.Version200.Value, ObjectTypes.GetVersion(typeof(Witsml200.Well)));
        }

        [TestMethod]
        public void ObjectTypes_GetGrowingObjectType_Returns_Growing_Part_Type()
        {
            Assert.AreEqual(ObjectTypes.LogCurveInfo, ObjectTypes.GetGrowingObjectType(ObjectTypes.Log));
            Assert.AreEqual(ObjectTypes.TrajectoryStation, ObjectTypes.GetGrowingObjectType(ObjectTypes.Trajectory));
            Assert.AreEqual(ObjectTypes.GeologyInterval, ObjectTypes.GetGrowingObjectType(ObjectTypes.MudLog));
            Assert.IsNull(ObjectTypes.GetGrowingObjectType(ObjectTypes.Well));
        }

        [TestMethod]
        public void ObjectTypes_SingleToPlural_Adds_s_To_all_Data_Object_Types()
        {
            Assert.AreEqual("wells", ObjectTypes.SingleToPlural(ObjectTypes.Well));
            Assert.AreEqual("logs", ObjectTypes.SingleToPlural(ObjectTypes.Log));
            Assert.AreEqual("trajectorys", ObjectTypes.SingleToPlural(ObjectTypes.Trajectory));
            Assert.AreEqual("trajectories", ObjectTypes.SingleToPlural(ObjectTypes.Trajectory, false));
        }

        [TestMethod]
        public void ObjectTypes_PluralToSingle_adds_s_To_All_Data_Object_Types()
        {
            Assert.AreEqual(ObjectTypes.Well, ObjectTypes.PluralToSingle("wells"));
            Assert.AreEqual(ObjectTypes.Log, ObjectTypes.PluralToSingle("logs"));
            Assert.AreEqual(ObjectTypes.Trajectory, ObjectTypes.PluralToSingle("trajectorys"));
            Assert.AreEqual(ObjectTypes.Trajectory, ObjectTypes.PluralToSingle("trajectories"));
        }
    }
}
