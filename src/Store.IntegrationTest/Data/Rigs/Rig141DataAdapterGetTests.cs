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
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Rigs
{
    /// <summary>
    /// Rig141DataAdapterGetTests
    /// </summary>
    [TestClass]
    public partial class Rig141DataAdapterGetTests : Rig141TestBase
    {
        private string _commonString;
        private string _bopPrefix;
        private string _namePrefix;
        private string _manufacturerPrefix;

        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            _commonString = DevKit.RandomString(5);
            _bopPrefix = $"bopComp-{_commonString}";
            _namePrefix = $"name-{_commonString}";
            _manufacturerPrefix = $"manufacturer-{_commonString}";
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_Multiple_Bop_Manufacturers_Returns_0_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            var manufacturerNumbers = new[] { "10", "30", "40" };
            const int expectedRigCount = 0;

            // Expected results: 0 rigs (No match on manufacturer)
            QueryAndAssertByBopManufacturer(manufacturerNumbers, expectedRigCount);
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_Multiple_Bop_Manufacturers_Returns_2_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            var manufacturerNumbers = new[] { "1", "3", "4" };
            const int expectedRigCount = 2;

            // Expected results: 2 rigs (match on manufacturer 1 & 3)
            QueryAndAssertByBopManufacturer(manufacturerNumbers, expectedRigCount);
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_Multiple_Bop_Manufacturers_Returns_3_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            var manufacturerNumbers = new[] { "1", "3", "4", "2" };
            const int expectedRigCount = 3;

            // Expected results: 2 rigs (match on manufacturer 1, 2 & 3)
            QueryAndAssertByBopManufacturer(manufacturerNumbers, expectedRigCount);
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_One_Name_Tag_With_Mismatched_Multi_Elements_Returns_0_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            // Expected Result: 0 Rigs have name #5 with Comment #6
            QueryAndAssertOneNameTagMultiElements(0, "5", "6", NameTagNumberingScheme.SerialNumber);
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_One_Name_Tag_With_Matched_Multi_Elements_Returns_2_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            // Expected Result: 2 Rigs (2 rigs with name #5)
            QueryAndAssertOneNameTagMultiElements(2, "5", "5", NameTagNumberingScheme.SerialNumber);
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_Multi_Name_Tag_With_Matched_Multi_Elements_Returns_2_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            var nameCommentNumbers = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("5","5"),     // Matches 2 rigs with Serial Number
                new Tuple<string, string>("6","6"),     
                new Tuple<string, string>("12","12"),   // Matches 1 rig on names, but NumberingScheme is Unknown
                new Tuple<string, string>("13","13"),   
            };

            // Expected Result: 2 Rigs have name #5 & #6 name and comment.
            //... Rig with #12 & #13 name and comment has NumberingScheme of Unknown
            QueryAndAssertMultiNameTagMultiElements(2, 2, nameCommentNumbers,NameTagNumberingScheme.SerialNumber );
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_Multi_BopComponent_With_Matched_Multi_Elements_Returns_2_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            var uidDescNumbers = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("5","5"),     // Matches 1 rigs with BopType.annularpreventer
                new Tuple<string, string>("6","6"),
                new Tuple<string, string>("12","12"),   // Matches 1 rig with BopType.annularpreventer
                new Tuple<string, string>("13","13"),
            };

            // Expected Result: 2 Rigs have #5 & #6 uid and desc, but only one is BopType.annularpreventer
            //... Rig with #12 & #13 uid and desc has with BopType.annularpreventer
            QueryAndAssertMultiBopComponentMultiElements(2, 2, uidDescNumbers, BopType.annularpreventer);
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_One_BopComponent_With_Matched_Multi_Elements_Returns_1_Rig()
        {
            AddAndAssertMultiRigForRecurringTests();

            // Expected Result: 1 Rig.  2 rigs have #5 uid and desc, but only one is BopType.annularpreventer
            QueryAndAssertOneBopComponentMultiElements(1, "5", "5", BopType.annularpreventer);
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_One_BopComponent_With_Mismatched_Multi_Elements_Returns_0_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            // Expected Result: 0 Rigs.  No rigs have #5 uid and #6 desc
            QueryAndAssertOneBopComponentMultiElements(0, "5", "6", BopType.annularpreventer);
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_One_BopComponent_One_NameTag_Multi_Elements_Returns_0_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            // Expected Result: 0 Rigs - No one Rig has #1 Bop Comp and #7 Name Tag 
            QueryAndAssertOneBopComponentOneNameTagMultiElements(0, "1", "7");
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_One_BopComponent_One_NameTag_Multi_Elements_Returns_1_Rig()
        {
            AddAndAssertMultiRigForRecurringTests();

            // Expected Result: 1 Rig - 1 Rigs have #1 Bop Comp and #5 Name Tag 
            QueryAndAssertOneBopComponentOneNameTagMultiElements(1, "1", "5");
        }

        [TestMethod]
        public void Rig141DataProvider_GetFromStore_By_One_BopComponent_One_NameTag_Multi_Elements_Returns_2_Rigs()
        {
            AddAndAssertMultiRigForRecurringTests();

            // Expected Result: 2 Rigs - 2 Rigs have #5 Bop Comp and Name Tag 
            QueryAndAssertOneBopComponentOneNameTagMultiElements(2, "5", "5");
        }

        #region Private Methods

        private void QueryAndAssertOneNameTagMultiElements(int expectedRigCount, string nameNumber, string commentNumber,
            NameTagNumberingScheme numberingScheme)
        {
            var nameCommentNumbers = new List<Tuple<string, string>>() { new Tuple<string, string>(nameNumber, commentNumber) };
            QueryAndAssertMultiNameTagMultiElements(expectedRigCount, 1, nameCommentNumbers, numberingScheme);
        }

        private void QueryAndAssertMultiNameTagMultiElements(int expectedRigCount, int expectedNameCount, List<Tuple<string, string>> nameCommentNumbers, NameTagNumberingScheme numberingScheme)
        {
            var query = new Rig
            {
                Bop = new Bop
                {
                    NameTag = new List<NameTag>()
                }
            };

            nameCommentNumbers.ForEach(n =>
            {
                query.Bop.NameTag.Add(new NameTag
                {
                    Name = $"{_namePrefix}-{n.Item1}",
                    NumberingScheme = numberingScheme,
                    Comment = $"{_namePrefix}-{n.Item2}"
                });
            });

            // Expected Result: expectedRigCount Rigs
            var results = DevKit.Query<RigList, Rig>(query, ObjectTypes.GetObjectType<RigList>(), null, OptionsIn.ReturnElements.All);
            Assert.AreEqual(expectedRigCount, results.Count);

            // Each rig that is returned should have expectedNameCount name and 6 BopComponents
            results.ForEach(r =>
            {
                Assert.AreEqual(expectedNameCount, r.Bop.NameTag.Count);
                Assert.AreEqual(6, r.Bop.BopComponent.Count);
            });
        }

        private void QueryAndAssertOneBopComponentMultiElements(int expectedRigCount, string uidNumber, string descNumber, BopType bopType)
        {
            var nameCommentNumbers = new List<Tuple<string, string>>() { new Tuple<string, string>(uidNumber, descNumber) };
            QueryAndAssertMultiBopComponentMultiElements(expectedRigCount, 1, nameCommentNumbers, bopType);
        }

        private void QueryAndAssertMultiBopComponentMultiElements(int expectedRigCount, int expectedComponentCount, List<Tuple<string, string>> uidDescNumbers, BopType bopType)
        {
            var query = new Rig
            {
                Bop = new Bop
                {
                    BopComponent = new List<BopComponent>()
                }
            };

            uidDescNumbers.ForEach(n =>
            {
                query.Bop.BopComponent.Add(new BopComponent
                {
                    Uid = $"{_bopPrefix}-{n.Item1}",
                    TypeBopComp = bopType,
                    DescComp = $"{_bopPrefix}-{n.Item2}"
                });
            });

            // Expected Result: expectedRigCount Rigs
            var results = DevKit.Query<RigList, Rig>(query, ObjectTypes.GetObjectType<RigList>(), null, OptionsIn.ReturnElements.All);
            Assert.AreEqual(expectedRigCount, results.Count);

            // Each rig that is returned should have expectedComponentCount BopComponents and 6 NameTags
            results.ForEach(r =>
            {
                Assert.AreEqual(expectedComponentCount, r.Bop.BopComponent.Count);
                Assert.AreEqual(6, r.Bop.NameTag.Count);
            });
        }

        private void QueryAndAssertByBopManufacturer(string[] manufacturerNumbers, int expectedRigCount)
        {
            var query = manufacturerNumbers
                .Select(manufacturerNumber => new Rig {Bop = new Bop {Manufacturer = $"{_manufacturerPrefix}-{manufacturerNumber}"}})
                .ToList();

            // Expected results: expectedRigCount rigs
            var results = DevKit.Query<RigList, Rig>(query, ObjectTypes.GetObjectType<RigList>(), null, OptionsIn.ReturnElements.All);
            Assert.AreEqual(expectedRigCount, results.Count);

            // Since we are filtering on BOP Manufacturer the BOP should contain all of the NameTags and BopComponents
            results.ForEach(r =>
            {
                Assert.AreEqual(6, r.Bop.BopComponent.Count);
                Assert.AreEqual(6, r.Bop.NameTag.Count);
            });
        }

        private void AddAndAssertMultiRigForRecurringTests()
        {
            AddParents();

            var rigNamePrefix = "Rig Recurring Test";
            var rig1 = DevKit.CreateRig(rigNamePrefix, Wellbore, DevKit.Bop(1, 1, $"{_manufacturerPrefix}-1"));
            var rig2 = DevKit.CreateRig(rigNamePrefix, Wellbore, DevKit.Bop(1, 1, $"{_manufacturerPrefix}-2"));
            var rig3 = DevKit.CreateRig(rigNamePrefix, Wellbore, DevKit.Bop(1, 1, $"{_manufacturerPrefix}-3"));

            // 1 - 6
            rig1.Bop.BopComponent = DevKit.BopComponents(1, 6, BopType.annularpreventer, _bopPrefix);
            rig1.Bop.NameTag = DevKit.NameTags(1, 6, _namePrefix, NameTagNumberingScheme.SerialNumber);

            // 5 - 10
            rig2.Bop.BopComponent = DevKit.BopComponents(5, 6, BopType.connector, _bopPrefix);
            rig2.Bop.NameTag = DevKit.NameTags(5, 6, _namePrefix, NameTagNumberingScheme.SerialNumber);

            // 12 - 17
            rig3.Bop.BopComponent = DevKit.BopComponents(12, 6, BopType.annularpreventer, _bopPrefix);
            rig3.Bop.NameTag = DevKit.NameTags(12, 6, _namePrefix, NameTagNumberingScheme.Unknown);

            DevKit.AddAndAssert(rig1);
            DevKit.AddAndAssert(rig2);
            DevKit.AddAndAssert(rig3);
        }

        private void QueryAndAssertOneBopComponentOneNameTagMultiElements(int expectedRigCount, string bopCompNumber, string nameNumber)
        {
            var query = new Rig
            {
                Bop = new Bop
                {
                    BopComponent = new List<BopComponent>
                    {
                        new BopComponent
                        {
                            Uid = $"{_bopPrefix}-{bopCompNumber}",
                            DescComp = $"{_bopPrefix}-{bopCompNumber}"
                        }
                    },
                    NameTag = new List<NameTag>
                    {
                        new NameTag
                        {
                            Name = $"{_namePrefix}-{nameNumber}",
                            Comment = $"{_namePrefix}-{nameNumber}"
                        }
                    }
                }
            };


            // Expected Result: expectedRigCount Rigs
            var results = DevKit.Query<RigList, Rig>(query, ObjectTypes.GetObjectType<RigList>(), null, OptionsIn.ReturnElements.All);
            Assert.AreEqual(expectedRigCount, results.Count);

            // Each rig that is returned should have 1 name and 1 BopComponents
            results.ForEach(r =>
            {
                Assert.AreEqual(1, r.Bop.NameTag.Count);
                Assert.AreEqual(1, r.Bop.BopComponent.Count);
            });
        }
        #endregion
    }
}
