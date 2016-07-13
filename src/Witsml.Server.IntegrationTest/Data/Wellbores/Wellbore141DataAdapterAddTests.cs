//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Wellbore141DataAdapter Add tests.
    /// </summary>
    [TestClass]
    public class Wellbore141DataAdapterAddTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well {
                Uid = _devKit.Uid(),
                Name = _devKit.Name("Well 01"),
                TimeZone = _devKit.TimeZone
            };

            _wellbore = new Wellbore()
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _devKit = null;
        }

        [TestMethod]
        public void Wellbore141DataAdapter_AddToStore_Can_Add_Wellbore()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully with dTimKickoff specified
        /// </summary>
        [TestMethod]
        public void Wellbore141DataAdapter_AddToStore_Can_Add_Wellbore_with_dTimKickoff()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            _wellbore.DateTimeKickoff = DateTimeOffset.Now;            
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_AddToStore_Can_Add_Wellbore_With_Same_Uid_Under_Different_Well()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var well2 = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well-to-add-02"), TimeZone = _devKit.TimeZone };
            response = _devKit.Add<WellList, Well>(well2);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore2 = new Wellbore()
            {
                Uid = well2.Uid,
                UidWell = response.SuppMsgOut,
                NameWell = well2.Name,
                Name = _devKit.Name("Wellbore 02-01")
            };
            response = _devKit.Add<WellboreList, Wellbore>(wellbore2);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
