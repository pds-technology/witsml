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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Providers.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;

using WbGeometry = Energistics.DataAccess.WITSML141.StandAloneWellboreGeometry;
using WbGeometryList = Energistics.DataAccess.WITSML141.WellboreGeometryList;


using System.Diagnostics;

namespace PDS.WITSMLstudio.Store.Providers.Discovery
{
    [TestClass()]
    public class Witsml131ProviderTests : IntegrationTestBase
    {
        Witsml131Provider _actual;

        public DevKit141Aspect DevKit { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            Logger.Debug($"Executing {TestContext.TestName}");
            DevKit = new DevKit141Aspect(TestContext);

            EtpSetUp(DevKit.Container);
            //_server.Start();
        }

        [TestCleanup]
        public void TestTearDown()
        {
            _server?.Stop();
            EtpCleanUp();
        }

        [TestMethod()]
        public void Witsml131Provider_Witsml131Provider()
        {
            var providers = DevKit.Container.ResolveAll<PDS.WITSMLstudio.Store.Providers.Discovery.IDiscoveryStoreProvider>();
            Assert.IsNotNull(providers);

            var list = providers.ToList();
            Assert.IsTrue(list.Any());

            foreach (var provider in list)
                Debug.WriteLine(provider.DataSchemaVersion);

            var actual = list.FirstOrDefault(x => x.DataSchemaVersion == "1.3.1.1") as Providers.Discovery.Witsml131Provider;
            Assert.IsNotNull(actual);

            _actual = actual;
        }

        [TestMethod()]
        public void Witsml131Provider_GetResources()
        {
            // TODO:
        }

        [TestMethod()]
        public void Witsml131Provider_GetResources1()
        {
            // TODO: 
        }

        [TestMethod()]
        public void Witsml131Provider_FindResources()
        {
            // TODO: 
        }

        /// <summary>
        /// Technically speaking it is not a test.
        /// The code shows the technique to be used in automatic tests
        /// </summary>
        [TestMethod]
        public void Witsml131Provider_TypeList()
        {
            Witsml131Provider_Witsml131Provider();

            {
                var adapters = GetDataAdapters<IWellboreObject>(_actual.Providers).ToList();
                foreach (var x in adapters)
                {
                    System.Type t = x.DataObjectType;
                    string t_name = ObjectTypes.GetObjectType(t);
                    Debug.WriteLine($"{t.Name} -> {t_name}");

                    IWellboreObject instance = (IWellboreObject)Activator.CreateInstance(t);
                    Assert.AreEqual(t.Name, instance.GetType().Name);
                }
                /*
                    BhaRun -> bhaRun
                    CementJob -> cementJob
                    ConvCore -> convCore
                    FluidsReport -> fluidsReport
                    FormationMarker -> formationMarker
                    Log -> log
                    Message -> message
                    MudLog -> mudLog
                    OpsReport -> opsReport
                    Rig -> rig
                    Risk -> risk
                    SidewallCore -> sidewallCore
                    SurveyProgram -> surveyProgram
                    Target -> target
                    Trajectory -> trajectory
                    Tubular -> tubular
                    StandAloneWellboreGeometry -> wbGeometry
                    WellLog -> wellLog
                */
            }
        }

        [TestMethod]
        public void Witsml131Provider_IsValidUri()
        {
            Witsml131Provider_Witsml131Provider();

            var access = new PrivateObject(_actual);
            {
                string[] expected_typeNames = { "BhaRun", "CementJob", "ConvCore", "FluidsReport",
                    "FormationMarker", "Log", "Message", "MudLog", "OpsReport", "Rig", "Risk", "SidewallCore",
                    "SurveyProgram", "Target", "Trajectory", "Tubular", "StandAloneWellboreGeometry",
                    "WellLog", };
                Assert.AreEqual(18, expected_typeNames.Length);

                var adapters = GetDataAdapters<IWellboreObject>(_actual.Providers).ToList();
                //Assert.AreEqual(23, adapters.Count);

                var actual_typeNames = adapters.Select(x => x.DataObjectType.Name).ToArray();
                CollectionAssert.AreEqual(expected_typeNames, actual_typeNames);

                foreach (var x in adapters)
                {
                    System.Type t = x.DataObjectType;
                    IWellboreObject instance = (IWellboreObject)Activator.CreateInstance(t);
                    instance.Uid = "abc"; instance.UidWell = "abc"; instance.UidWellbore = "abc";

                    var uri = instance.GetUri();
                    var parentUri = uri.Parent;
                    var folderUri = parentUri.Append(uri.ObjectType);
                    bool valid = (bool)access.Invoke("IsValidUri", folderUri);
                    Assert.IsTrue(valid, t.Name);
                }
            }
        }

        static private IEnumerable<Data.IWitsmlDataAdapter> GetDataAdapters<TObject>(IEnumerable<Configuration.IWitsml131Configuration> providers)
        {
            var objectType = typeof(TObject);

            return providers
                .OfType<Data.IWitsmlDataAdapter>()
                .Where(x => objectType.IsAssignableFrom(x.DataObjectType))
                .OrderBy(x => x.GetType().Name);
        }
    }
}