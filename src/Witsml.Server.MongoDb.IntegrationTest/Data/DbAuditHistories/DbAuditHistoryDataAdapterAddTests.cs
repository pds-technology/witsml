//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Data.ChangeLogs;
using PDS.Witsml.Server.Data.ChangeLogs;

namespace PDS.Witsml.Server.Data.DbAuditHistories
{
    [TestClass]
    public class DbAuditHistoryDataAdapterAddTests
    {
        private DevKit141Aspect _devKit;
        public TestContext TestContext { get; set; }

        private IWitsmlDataProvider _dataProvider;
        private IWitsmlDataAdapter<DbAuditHistory> _dataAdapter;

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);
            _dataProvider = _devKit.Container.Resolve<IWitsmlDataProvider>(ObjectNames.ChangeLog141);
            _dataAdapter = _devKit.Container.Resolve<IWitsmlDataAdapter<DbAuditHistory>>();

        }

        [TestMethod]
        public void DbAuditHistory_Test_Add()
        {
            var changeLog = new DbAuditHistory() {Uid = Guid.NewGuid().ToString(), Name = "Test"};

            _dataAdapter.Add(null, changeLog);

            Assert.IsNotNull(changeLog);
        }
    }
}
