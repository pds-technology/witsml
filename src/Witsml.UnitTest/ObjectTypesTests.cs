using System;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace PDS.Witsml
{
    [TestClass]
    public class ObjectTypesTests
    {
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
    }
}
