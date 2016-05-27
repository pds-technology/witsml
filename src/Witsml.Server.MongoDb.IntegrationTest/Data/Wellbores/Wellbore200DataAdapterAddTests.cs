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

using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Server.Data.Wells;

namespace PDS.Witsml.Server.Data.Wellbores
{
    [TestClass]
    public class Wellbore200DataAdapterAddTests
    {
        private DevKit200Aspect DevKit;
        private IDatabaseProvider Provider;
        private IWitsmlDataAdapter<Well> WellAdapter;
        private IWitsmlDataAdapter<Wellbore> WellboreAdapter;

        private Well Well1;
        private Well Well2;
        private Wellbore Wellbore1;
        private Wellbore Wellbore2;
        private DataObjectReference WellReference;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect(TestContext);
            Provider = DevKit.Container.Resolve<IDatabaseProvider>();

            WellAdapter = new Well200DataAdapter(Provider);
            WellboreAdapter = new Wellbore200DataAdapter(Provider);

            Well1 = new Well() { Citation = DevKit.Citation("Well 01"), TimeZone = DevKit.TimeZone, Uuid = DevKit.Uid() };
            Well2 = new Well() { Citation = DevKit.Citation("Well 02"), TimeZone = DevKit.TimeZone, Uuid = DevKit.Uid() };

            Well1.GeographicLocationWGS84 = DevKit.Location();
            Well2.GeographicLocationWGS84 = DevKit.Location();

            WellReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Well),
                Title = Well1.Citation.Title,
                Uuid = Well1.Uuid
            };

            Wellbore1 = new Wellbore() { Citation = DevKit.Citation("Wellbore 01"), ReferenceWell = WellReference, Uuid = DevKit.Uid() };
            Wellbore2 = new Wellbore() { Citation = DevKit.Citation("Wellbore 02"), ReferenceWell = WellReference };
        }

        [TestMethod]
        public void CanAddAndGetSingleWellboreWithUuid()
        {
            WellAdapter.Add(DevKit.Parser(Well1), Well1);
            WellboreAdapter.Add(DevKit.Parser(Wellbore1), Wellbore1);

            var wellbore1 = WellboreAdapter.Get(Wellbore1.GetUri());

            Assert.AreEqual(Wellbore1.Citation.Title, wellbore1.Citation.Title);
        }

        [TestMethod]
        public void CanAddAndGetSingleWellboreWithoutUuid()
        {
            WellAdapter.Add(DevKit.Parser(Well1), Well1);
            WellboreAdapter.Add(DevKit.Parser(Wellbore2), Wellbore2);

            var wellbore2 = Provider.GetDatabase().GetCollection<Wellbore>(ObjectNames.Wellbore200).AsQueryable()
                .First(x => x.Citation.Title == Wellbore2.Citation.Title);

            Assert.AreEqual(Wellbore2.Citation.Title, wellbore2.Citation.Title);
        }
    }
}
