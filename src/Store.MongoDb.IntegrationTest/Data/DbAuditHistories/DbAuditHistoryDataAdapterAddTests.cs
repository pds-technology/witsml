//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System.Collections.Generic;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Data.ChangeLogs;

namespace PDS.Witsml.Server.Data.DbAuditHistories
{
    [TestClass]
    public class DbAuditHistoryDataAdapterAddTests
    {
        public TestContext TestContext { get; set; }

        private DevKit141Aspect _devKit;
        private IWitsmlDataAdapter<DbAuditHistory> _dataAdapter;
        private DbAuditHistory _changeLog;

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);
            _dataAdapter = _devKit.Container.Resolve<IWitsmlDataAdapter<DbAuditHistory>>();

            _changeLog = new DbAuditHistory
            {
                Uid = _devKit.Uid(),
                Name = _devKit.Name(),
                UidObject = _devKit.Uid(),
                NameObject = _devKit.Name(),
                ObjectType = ObjectTypes.Well,
                LastChangeType = ChangeInfoType.add,
                LastChangeInfo = $"Well was added for test {TestContext.TestName}",
                ChangeHistory = new List<ChangeHistory>()
            };

            _changeLog.CommonData = _changeLog.CommonData.Create();

            _changeLog.ChangeHistory.Add(new ChangeHistory
            {
                ChangeInfo = _changeLog.LastChangeInfo,
                ChangeType = _changeLog.LastChangeType,
                DateTimeChange = _changeLog.CommonData.DateTimeLastChange
            });
        }

        [TestMethod]
        public void DbAuditHistory_Test_Add()
        {
            _dataAdapter.Add(null, _changeLog);

            var changeLog = _dataAdapter.Get(_changeLog.GetUri());

            Assert.IsNotNull(changeLog);
            Assert.AreEqual(_changeLog.Uid, changeLog.Uid);
            Assert.AreEqual(_changeLog.Name, changeLog.Name);
            Assert.AreEqual(_changeLog.UidObject, changeLog.UidObject);
            Assert.AreEqual(_changeLog.NameObject, changeLog.NameObject);
            Assert.AreEqual(_changeLog.LastChangeType, changeLog.LastChangeType);
        }
    }
}
