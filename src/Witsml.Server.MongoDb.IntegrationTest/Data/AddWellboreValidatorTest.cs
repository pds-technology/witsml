using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML141;
using PDS.Framework;
using PDS.Witsml.Server.Data.Wellbores;
using MongoDB.Driver;
using PDS.Witsml.Server.Data.Wells;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// AddWellboreValidator test class
    /// </summary>
    [TestClass]
    public class AddWellboreValidatorTest
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
        private string TestUidWellBore;

        [TestInitialize]
        public void TestSetUp()
        {
            Provider = new DatabaseProvider(Mapper);
            DevKit = new DevKit141Aspect(null);

            DataAdaptor = new Wellbore141DataAdapter(Provider);
            WellDataAdaptor = new Well141DataAdapter(Provider);

            TestUidWell = DevKit.Uid();
            TestUidWellBore = DevKit.Uid();
            TestWell = new Well() { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone, Uid = TestUidWell };
            TestWellbore = new Wellbore() { Name = DevKit.Name("Wellbore 01-01"), UidWell = TestUidWell, Uid = TestUidWellBore };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Provider = null;
            DevKit = null;

            DataAdaptor = null;
            WellDataAdaptor = null;

            TestUidWell = string.Empty;
            TestUidWellBore = string.Empty;
            TestWell = null;
            TestWellbore = null;
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully
        /// </summary>
        [TestMethod]
        public void Validate_wellbore()
        {
            ICollection<ValidationResult> results;

            AddWell();

            var entity = new Wellbore()
            {
                UidWell = TestUidWell,
                Uid = TestUidWellBore,
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };

            var addWellboreValidator = new AddWellboreValidator(DataAdaptor, WellDataAdaptor);
            addWellboreValidator.DataObject = entity;
            var result = EntityValidator.TryValidate(addWellboreValidator, out results);

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> to an non-existing well.
        /// </summary>
        [TestMethod]
        public void Validate_wellbore_MissingParentObject()
        {
            ICollection<ValidationResult> results;

            var entity = new Wellbore()
            {
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };

            var addWellboreValidator = new AddWellboreValidator(DataAdaptor, WellDataAdaptor);
            addWellboreValidator.DataObject = entity;
            var result = EntityValidator.TryValidate(addWellboreValidator, out results);

            Assert.IsFalse(result);
            Assert.IsTrue(results.Count == 1);
            Assert.IsTrue(results.ToArray().First().ErrorMessage.Equals(ErrorCodes.MissingParentDataObject.ToString()));
        }

        /// <summary>
        /// Test adding an existing <see cref="Wellbore"/> 
        /// </summary>
        [TestMethod]
        public void Validate_wellbore_DataObjectUidAlreadyExists()
        {
            ICollection<ValidationResult> results;
           
            AddWell();
            AddWellbore();

            var entity = new Wellbore()
            {
                UidWell = TestUidWell,
                Uid = TestUidWellBore,
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };

            var addWellboreValidator = new AddWellboreValidator(DataAdaptor, WellDataAdaptor);
            addWellboreValidator.DataObject = entity;
            var result = EntityValidator.TryValidate(addWellboreValidator, out results);

            Assert.IsFalse(result);
            Assert.IsTrue(results.Count == 1);
            Assert.IsTrue(results.ToArray().First().ErrorMessage.Equals(ErrorCodes.DataObjectUidAlreadyExists.ToString()));
        }

        public void AddWell()
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

        public void AddWellbore()
        {
            var database = Provider.GetDatabase();
            var collectionWB = database.GetCollection<Wellbore>(ObjectNames.Wellbore141);

            collectionWB.InsertMany(new[] { TestWellbore });

            var resultWB = collectionWB.AsQueryable()
                .Where(x => x.Uid == TestWellbore.Uid)
                .ToList();

            Assert.IsNotNull(resultWB);
            Assert.AreEqual(1, resultWB.Count);
            Assert.AreEqual(TestWellbore.Name, resultWB[0].Name);
        }

    }
}
