using System;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace PDS.Witsml
{
    [TestClass]
    public class ObjectTypesTests
    {
        private string _wellsXml =
                "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + Environment.NewLine +
                "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\" />";

        [TestMethod]
        public void GetObjectType_returns_valid_witsml_type_for_Well()
        {
            const string expected = ObjectTypes.Well;

            var actual = ObjectTypes.GetObjectType<WellList>();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetObjectType_returns_valid_witsml_type_for_Wellbore()
        {
            const string expected = ObjectTypes.Wellbore;

            var actual = ObjectTypes.GetObjectType(typeof(WellboreList));

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetObjectType_returns_null_for_invalid_witsml_type()
        {
            Should.Throw<ArgumentException>(() =>
            {
                ObjectTypes.GetObjectType(typeof(Well));
            });
        }

        [TestMethod]
        public void GetObjectTypeFromGroup_returns_type_for_valid_xml()
        {
            var typeFound = ObjectTypes.GetObjectTypeFromGroup(_wellsXml);

            Assert.AreEqual(ObjectTypes.Well, typeFound);
        }

        [TestMethod]
        public void GetObjectTypeFromGroup_returns_unknown_for_invalid_xml()
        {
            var typeFound = ObjectTypes.GetObjectTypeFromGroup(string.Empty);

            Assert.AreEqual(ObjectTypes.Unknown, typeFound);
        }
    }
}
