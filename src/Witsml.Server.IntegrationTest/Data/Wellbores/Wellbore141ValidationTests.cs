using System;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// AddWellboreValidator test class
    /// </summary>
    [TestClass]
    public class Wellbore141ValidationTests
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

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully
        /// </summary>
        [TestMethod]
        public void Validate_wellbore()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            var wellbore = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);   
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully with dTimKickoff specified
        /// </summary>
        [TestMethod]
        public void Validate_wellbore_with_dTimKickoff()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            var wellbore = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = well.Name,
                Name = "Wellbore 01-01",
                DateTimeKickoff = DateTimeOffset.Now
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        /// <summary>
        /// Test adding an existing <see cref="Wellbore"/> 
        /// </summary>
        [TestMethod]
        public void Test_error_code_405_data_object_uid_duplicate()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = "Wellbore-to-add-01", NameWell = well.Name, UidWell = response.SuppMsgOut, Uid = DevKit.Uid() };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidAlreadyExists, response.Result);
        }

        [TestMethod]
        public void Test_error_code_406_missing_parent_uid()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = "Wellbore-to-add-01", NameWell = well.Name };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingParentUid, response.Result);         
        }

        [Ignore]
        [TestMethod]
        public void Test_error_code_478_parent_uid_case_not_matching()
        {
            var uid = "arent-well-01-for-error-code-478";
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone, Uid = "P" + uid};
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = "Wellbore-to-add-01", NameWell = well.Name, UidWell = well.Uid };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            wellbore = new Wellbore { Name = "Wellbore-to-add-02", NameWell = well.Name, UidWell = "p" + uid };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.IncorrectCaseParentUid, response.Result);
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> to an non-existing well.
        /// </summary>
        [TestMethod]
        public void Test_error_code_481_missing_parent_object()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var wellbore = new Wellbore { Name = "Wellbore-to-add-01", NameWell = well.Name, UidWell = DevKit.Uid() };
            var response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingParentDataObject, response.Result);
        }

        [TestMethod]
        public void Test_can_add_wellbore_with_same_uid_under_different_well()
        {
            var well1 = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well1);

            var wellbore1 = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = well1.Name,
                Name = "Wellbore 01-01"
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore1);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var well2 = new Well { Name = "Well-to-add-02", TimeZone = DevKit.TimeZone };
            response = DevKit.Add<WellList, Well>(well2);

            var wellbore2 = new Wellbore()
            {
                Uid = uid,
                UidWell = response.SuppMsgOut,
                NameWell = well2.Name,
                Name = "Wellbore 02-01"
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore2);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
