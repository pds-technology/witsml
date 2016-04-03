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

using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PDS.Framework;
using PDS.Witsml.Server.Data.Wells;

namespace PDS.Witsml.Server.Data.Wellbores
{
    [TestClass]
    public class Wellbore200DataAdapterTests
    {
        private DevKit200Aspect DevKit;
        private IContainer Container;
        private IDatabaseProvider Provider;
        private IEtpDataAdapter<Well> WellAdapter;
        private IEtpDataAdapter<Wellbore> WellboreAdapter;

        private Well Well1;
        private Well Well2;
        private Wellbore Wellbore1;
        private Wellbore Wellbore2;
        private DataObjectReference WellReference;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect();
            Container = ContainerFactory.Create();
            Provider = new DatabaseProvider(new MongoDbClassMapper());

            WellAdapter = new Well200DataAdapter(Provider) { Container = Container };
            WellboreAdapter = new Wellbore200DataAdapter(Provider) { Container = Container };

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
            WellAdapter.Put(DevKit.Parser(Well1));
            WellboreAdapter.Put(DevKit.Parser(Wellbore1));

            var wellbore1 = WellboreAdapter.Get(Wellbore1.GetUri());

            Assert.AreEqual(Wellbore1.Citation.Title, wellbore1.Citation.Title);
        }

        [TestMethod]
        public void CanAddAndGetSingleWellboreWithoutUuid()
        {
            WellAdapter.Put(DevKit.Parser(Well1));
            WellboreAdapter.Put(DevKit.Parser(Wellbore2));

            var wellbore2 = Provider.GetDatabase().GetCollection<Wellbore>(ObjectNames.Wellbore200).AsQueryable()
                .Where(x => x.Citation.Title == Wellbore2.Citation.Title)
                .FirstOrDefault();

            Assert.AreEqual(Wellbore2.Citation.Title, wellbore2.Citation.Title);
        }
    }
}
