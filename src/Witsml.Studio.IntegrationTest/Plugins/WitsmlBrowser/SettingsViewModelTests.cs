//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
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

using Energistics.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.Connections;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser
{
    [TestClass]
    public class SettingsViewModelTests
    {
        const string _validWitsmlUri = "http://localhost/Witsml.Web/WitsmlStore.svc";
        private BootstrapperHarness _bootstrapper;
        private TestRuntimeService _runtime;
        private SettingsViewModel _settingsViewModel;

        [TestInitialize]
        public void TestSetUp()
        {
            _bootstrapper = new BootstrapperHarness();
            _runtime = new TestRuntimeService(_bootstrapper.Container);
            _runtime.Shell = new ShellViewModel(_runtime);
            _settingsViewModel = new SettingsViewModel(_runtime);
        }

        [TestMethod]
        public void Can_get_supported_versions()
        {
            WITSMLWebServiceConnection proxy = new WITSMLWebServiceConnection(_validWitsmlUri, WMLSVersion.WITSML141);
            Connection connection = new Connection() { Uri = _validWitsmlUri };

            var versions = _settingsViewModel.GetVersions(proxy, connection);
            Assert.IsTrue(!string.IsNullOrEmpty(versions));
        }
    }
}
