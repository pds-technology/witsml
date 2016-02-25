using System;
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
            var well = new Well { Name = "Well-to-add-01" };
            var response = DevKit.AddWell(well);

            var wellbore = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };
            response = DevKit.AddWellbore(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);   
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully with dTimKickoff specified
        /// </summary>
        [TestMethod]
        public void Validate_wellbore_with_dTimKickoff()
        {
            var well = new Well { Name = "Well-to-add-01" };
            var response = DevKit.AddWell(well);

            var wellbore = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = well.Name,
                Name = "Wellbore 01-01",
                DateTimeKickoff = DateTime.Now
            };
            response = DevKit.AddWellbore(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        /// <summary>
        /// Test adding an existing <see cref="Wellbore"/> 
        /// </summary>
        [TestMethod]
        public void Test_error_code_405_data_object_uid_duplicate()
        {
            var well = new Well { Name = "Well-to-add-01" };
            var response = DevKit.AddWell(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = "Wellbore-to-add-01", NameWell = well.Name, UidWell = response.SuppMsgOut, Uid = DevKit.Uid() };
            response = DevKit.AddWellbore(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = DevKit.AddWellbore(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidAlreadyExists, response.Result);
        }

        [TestMethod]
        public void Test_error_code_406_missing_parent_uid()
        {
            var well = new Well { Name = "Well-to-add-01" };
            var response = DevKit.AddWell(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = "Wellbore-to-add-01", NameWell = well.Name };
            response = DevKit.AddWellbore(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingParentUid, response.Result);         
        }

        [TestMethod]
        public void Test_error_code_478_parent_uid_case_not_matching()
        {
            var uid = "arent-well-01-for-error-code-478";
            var well = new Well { Name = "Well-to-add-01", Uid = "P" + uid};
            var response = DevKit.AddWell(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = "Wellbore-to-add-01", NameWell = well.Name, UidWell = well.Uid };
            response = DevKit.AddWellbore(wellbore);

            wellbore = new Wellbore { Name = "Wellbore-to-add-02", NameWell = well.Name, UidWell = "p" + uid };
            response = DevKit.AddWellbore(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.IncorrectCaseParentUid, response.Result);
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> to an non-existing well.
        /// </summary>
        [TestMethod]
        public void Test_error_code_481_missing_parent_object()
        {
            var well = new Well { Name = "Well-to-add-01" };
            var wellbore = new Wellbore { Name = "Wellbore-to-add-01", NameWell = well.Name, UidWell = DevKit.Uid() };
            var response = DevKit.AddWellbore(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingParentDataObject, response.Result);
        }
    }
}
