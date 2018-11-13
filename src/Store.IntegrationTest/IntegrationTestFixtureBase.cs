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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store
{
    /// <summary>
    /// Common base class fixture for all integration tests.
    /// </summary>
    public abstract class IntegrationTestFixtureBase<TDevKitAspect> : IntegrationTestBase where TDevKitAspect : DevKitAspect
    {
        private readonly bool _isEtpTest;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTestFixtureBase"/> class.
        /// </summary>
        protected IntegrationTestFixtureBase(bool isEtpTest)
            : base()
        {
            _isEtpTest = isEtpTest;
        }

        /// <summary>
        /// The DevKit aspect.
        /// </summary>
        public TDevKitAspect DevKit { get; private set; }

        /// <summary>
        /// Common test setup.
        /// </summary>
        /// <remarks>Calls <see cref="OnTestSetUp"/> for test-specific setup.</remarks>
        [TestInitialize]
        public void TestSetUp()
        {
            log4net.ThreadContext.Properties["TestName"] = TestContext.TestName;
            Logger.Info($"Setting up {TestContext.TestName}");
            DevKit = (TDevKitAspect)Activator.CreateInstance(typeof(TDevKitAspect), TestContext);

            PrepareData();
            OnTestSetUp();

            if (_isEtpTest)
            {
                EtpSetUp(DevKit.Container);
                _server.Start();
            }

            Logger.Info($"Setup is complete for {TestContext.TestName}");
        }

        /// <summary>
        /// Common test cleanup.
        /// </summary>
        /// <remarks>Calls <see cref="OnTestCleanUp"/> for test-specific cleanup.</remarks>
        [TestCleanup]
        public void TestCleanUp()
        {
            Logger.Info($"Cleaning up {TestContext.TestName}");
            if (_isEtpTest)
            {
                _server?.Stop();
                EtpCleanUp();
            }

            OnTestCleanUp();

            DevKit?.Container.Dispose();
            DevKit = null;

            DevKitAspect.RestoreDefaultSettings();
            WitsmlOperationContext.Current = null;

            Logger.Info($"Cleanup is complete for {TestContext.TestName}");
            log4net.ThreadContext.Properties["TestName"] = null;
        }

        /// <summary>
        /// Prepare common test data.
        /// </summary>
        protected virtual void PrepareData() { }

        /// <summary>
        /// Test-specific setup.
        /// </summary>
        protected virtual void OnTestSetUp() { }

        /// <summary>
        /// Test-specific cleanup.
        /// </summary>
        protected virtual void OnTestCleanUp() { }

    }
}
