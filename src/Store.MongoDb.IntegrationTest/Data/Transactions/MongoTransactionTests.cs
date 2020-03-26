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
using System.Reflection;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NConcern;
using PDS.WITSMLstudio.Store.Aspects;
using PDS.WITSMLstudio.Store.Data.Logs;
using PDS.WITSMLstudio.Store.Data.Wellbores;
using Shouldly;

namespace PDS.WITSMLstudio.Store.Data.Transactions
{
    [TestClass]
    public class MongoTransactionTests : MultiObject141TestBase
    {
        private const BindingFlags Scope = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        protected override void OnTestSetUp()
        {
        }

        protected override void OnTestCleanUp()
        {
        }

        [Ignore]
        [TestMethod]
        public void MongoTransaction_Rollback_Nested_Transaction_When_Error_Before_UpdateIsActive()
        {
            // Init
            AddParents();
            Log.IndexType = LogIndexType.measureddepth;
            Log.StartIndex = new GenericMeasure(5, "m");

            // AddToStore
            DevKit.InitHeader(Log, Log.IndexType.Value);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);
            DevKit.AddAndAssert(Log);

            var addedLog = DevKit.GetAndAssert(Log);
            var addedWellbore = DevKit.GetAndAssert(Wellbore);
            Assert.IsFalse(addedWellbore.IsActive.GetValueOrDefault(), "IsActive");
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            // UpdateInStore
            var update = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            update.StartIndex = new GenericMeasure(17, "m");

            DevKit.InitHeader(update, Log.IndexType.Value);
            DevKit.InitDataMany(update, DevKit.Mnemonics(update), DevKit.Units(update), 6);

            // Force exception to be thrown
            Throw.For<Wellbore141DataAdapter>().Before("UpdateIsActive");

            // Check for expected exception
            Should.Throw<Exception>(() => DevKit.UpdateAndAssert(update));

            // Remove forced exception
            Aspect.Release<Throw>();

            var savedLog = DevKit.GetAndAssert(Log);
            var savedWellbore = DevKit.GetAndAssert(Wellbore);
            Assert.IsFalse(savedWellbore.IsActive.GetValueOrDefault(), "IsActive");
            Assert.IsFalse(savedLog.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.AreEqual(Log.EndIndex.Value, savedLog.EndIndex.Value);
            DevKit.AssertChangeLog(savedLog, 1, ChangeInfoType.add);
        }

        [Ignore]
        [TestMethod]
        public void MongoTransaction_Rollback_Nested_Transaction_When_Error_After_Update()
        {
            // Init
            AddParents();
            Log.IndexType = LogIndexType.measureddepth;
            Log.StartIndex = new GenericMeasure(5, "m");

            // AddToStore
            DevKit.InitHeader(Log, Log.IndexType.Value);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);
            DevKit.AddAndAssert(Log);

            var addedLog = DevKit.GetAndAssert(Log);
            var addedWellbore = DevKit.GetAndAssert(Wellbore);
            Assert.IsFalse(addedWellbore.IsActive.GetValueOrDefault(), "IsActive");
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            // UpdateInStore
            var update = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            update.StartIndex = new GenericMeasure(17, "m");

            DevKit.InitHeader(update, Log.IndexType.Value);
            DevKit.InitDataMany(update, DevKit.Mnemonics(update), DevKit.Units(update), 6);

            // Force exception to be thrown
            Throw.For<Log141DataAdapter>().After("Update");

            // Check for expected exception
            Should.Throw<Exception>(() => DevKit.UpdateAndAssert(update));

            // Remove forced exception
            Aspect.Release<Throw>();

            var savedLog = DevKit.GetAndAssert(Log);
            var savedWellbore = DevKit.GetAndAssert(Wellbore);
            Assert.IsFalse(savedWellbore.IsActive.GetValueOrDefault(), "IsActive");
            Assert.IsFalse(savedLog.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.AreEqual(Log.EndIndex.Value, savedLog.EndIndex.Value);
            DevKit.AssertChangeLog(savedLog, 1, ChangeInfoType.add);
        }
    }
}
