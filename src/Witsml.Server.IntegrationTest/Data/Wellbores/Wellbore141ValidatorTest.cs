using System;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using PDS.Witsml.Server.Data.Wells;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// AddWellboreValidator test class
    /// </summary>
    [TestClass]
    public class Wellbore141ValidatorTest
    {
        private static readonly string ParentDbDocumentName = ObjectNames.Well141;
        private static readonly string DbDocumentName = ObjectNames.Wellbore141;

        private Mapper Mapper = new Mapper();
        private IDatabaseProvider Provider;
        private DevKit141Aspect DevKit;
        private Wellbore141DataAdapter DataAdaptor;
        private Well141DataAdapter WellDataAdaptor;

        private Well TestWell;
        private Wellbore TestWellbore;
        private string TestUidWell;
        private string TestUidWellbore;

        [TestInitialize]
        public void TestSetUp()
        {
            Provider = new DatabaseProvider(Mapper);
            DevKit = new DevKit141Aspect(null);

            DataAdaptor = new Wellbore141DataAdapter(Provider);
            WellDataAdaptor = new Well141DataAdapter(Provider);

            TestUidWell = DevKit.Uid();
            TestUidWellbore = DevKit.Uid();
            TestWell = new Well() { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone, Uid = TestUidWell };
            TestWellbore = new Wellbore() { Name = DevKit.Name("Wellbore 01-01"), UidWell = TestUidWell, Uid = TestUidWellbore };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Provider = null;
            DevKit = null;

            DataAdaptor = null;
            WellDataAdaptor = null;

            TestUidWell = string.Empty;
            TestUidWellbore = string.Empty;
            TestWell = null;
            TestWellbore = null;
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully
        /// </summary>
        [TestMethod]
        public void Validate_wellbore()
        {
            AddWell();

            var entity = new Wellbore()
            {
                UidWell = TestUidWell,
                Uid = TestUidWellbore,
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };

            var validator = new Wellbore141Validator(DataAdaptor, WellDataAdaptor);
            var results = validator.Validate(Functions.AddToStore, entity);

            Assert.IsFalse(results.Any());
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> to an non-existing well.
        /// </summary>
        [TestMethod]
        public void Validate_wellbore_MissingParentObject()
        {
            var entity = new Wellbore()
            {
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };

            var validator = new Wellbore141Validator(DataAdaptor, WellDataAdaptor);
            var results = validator.Validate(Functions.AddToStore, entity);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(ErrorCodes.MissingParentDataObject.ToString(), results.First().ErrorMessage);
        }

        /// <summary>
        /// Test adding an existing <see cref="Wellbore"/> 
        /// </summary>
        [TestMethod]
        public void Validate_wellbore_DataObjectUidAlreadyExists()
        {
            AddWell();
            AddWellbore();

            var entity = new Wellbore()
            {
                UidWell = TestUidWell,
                Uid = TestUidWellbore,
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };

            var validator = new Wellbore141Validator(DataAdaptor, WellDataAdaptor);
            var results = validator.Validate(Functions.AddToStore, entity);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(ErrorCodes.DataObjectUidAlreadyExists.ToString(), results.First().ErrorMessage);
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully with dTimKickoff specified
        /// </summary>
        [TestMethod]
        public void Validate_wellbore_with_dTimKickoff()
        {
            AddWell();

            var entity = new Wellbore()
            {
                UidWell = TestUidWell,
                Uid = TestUidWellbore,
                NameWell = "Well 01",
                Name = "Wellbore 01-01",
                DateTimeKickoff = DateTime.Now
            };

            var validator = new Wellbore141Validator(DataAdaptor, WellDataAdaptor);
            var results = validator.Validate(Functions.AddToStore, entity);

            Assert.IsFalse(results.Any());
        }

        private void AddWell()
        {
            var database = Provider.GetDatabase();
            var collection = database.GetCollection<Well>(ObjectNames.Well141);

            collection.InsertMany(new[] { TestWell });

            var result = collection.AsQueryable()
                .Where(x => x.Uid == TestWell.Uid)
                .ToList();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(TestWell.Name, result[0].Name);
        }

        private void AddWellbore()
        {
            var database = Provider.GetDatabase();
            var collection = database.GetCollection<Wellbore>(ObjectNames.Wellbore141);

            collection.InsertMany(new[] { TestWellbore });

            var result = collection.AsQueryable()
                .Where(x => x.Uid == TestWellbore.Uid)
                .ToList();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(TestWellbore.Name, result[0].Name);
        }

    }
}
