using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well141ValidationTests
    {
        private DevKit141Aspect DevKit;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DevKit = null;
        }

        [TestMethod]
        public void Test_error_code_438_recurring_elements_inconsistent_selection()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var crs1 = DevKit.WellCRS("geog1", null);
            var crs2 = DevKit.WellCRS(null, "ED50 / UTM Zone 31N");
            var query = new Well { Uid = "", WellCRS = DevKit.List(crs1, crs2) };
            var result = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void Test_error_code_439_recurring_elements_empty_value()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var crs1 = DevKit.WellCRS("geog1", string.Empty);
            var crs2 = DevKit.WellCRS("proj1", "ED50 / UTM Zone 31N");
            var query = new Well { Uid = "", WellCRS = DevKit.List(crs1, crs2) };
            var result = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, result.Result);
        }
    }
}
