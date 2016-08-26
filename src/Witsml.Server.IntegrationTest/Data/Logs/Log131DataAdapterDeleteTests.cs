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

using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log131DataAdapter Delete tests.
    /// </summary>
    [TestClass]
    public class Log131DataAdapterDeleteTests
    {
        private DevKit131Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;
        private Log _log;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit131Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version131.Value)
                .ToArray();

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };

            _wellbore = new Wellbore
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };

            _log = _devKit.CreateLog(_devKit.Uid(), _devKit.Name("Log 01"), _well.Uid, _well.Name, _wellbore.Uid, _wellbore.Name);
        }

        [TestMethod]
        public void Log131DataAdapter_DeleteFromStore_Can_Partial_Delete_Elements()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            _log.ServiceCompany = "company 1";
            _log.StepIncrement = new RatioGenericMeasure { Uom = "m", Value = 1.0 };

            // Add log
            _devKit.AddAndAssert(_log);

            // Assert all testing elements are added
            var result = _devKit.GetOneAndAssert(_log);
            Assert.AreEqual(_log.ServiceCompany, result.ServiceCompany);
            Assert.AreEqual(_log.Direction, result.Direction);

            // Partial delete well
            const string delete = "<serviceCompany /><stepIncrement />";
            DeleteLog(_log, delete);

            // Assert the well elements has been deleted
            result = _devKit.GetOneAndAssert(_log);
            Assert.IsNull(result.ServiceCompany);
            Assert.IsNull(result.StepIncrement);
        }

        #region Helper Methods

        private void AddParents()
        {
            _devKit.AddAndAssert(_well);
            _devKit.AddAndAssert(_wellbore);
        }

        private void DeleteLog(Log log, string delete, ErrorCodes error = ErrorCodes.Success)
        {
            var queryIn = string.Format(DevKit131Aspect.BasicDeleteLogXmlTemplate, log.Uid, log.UidWell, log.UidWellbore, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);
            Assert.AreEqual((short)error, response.Result);
        }

        #endregion
    }
}
