using System;
using System.Collections.Generic;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Server.Data;
using PDS.Witsml.Server.Data.Wells;

namespace PDS.Witsml.Server
{
    [TestClass]
    public class WitsmlStore141Tests
    {
        private WitsmlStore _witsmlStore;

        [TestInitialize]
        public void TestSetUp()
        {
            _witsmlStore = new WitsmlStore();
            _witsmlStore.Container = ContainerFactory.Create();
        }

        [TestMethod]
        public void Test_can_add_well_without_validation()
        {
            var well = new Well { Name = "Well-to-add-01", Uid = Uid() };
            var wells = new WellList { Well = new List<Well>() };
            wells.Well.Add(well);
            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var request = new WMLS_AddToStoreRequest { WMLtypeIn = "well", XMLin = xmlIn };
            var response = _witsmlStore.WMLS_AddToStore(request);

            Assert.IsNotNull(response);
            Assert.AreEqual(response.Result, (short)ErrorCodes.Success);
        }

        [TestMethod]
        public void Test_mongo_database_exception()
        {
            var dbProvider = new DatabaseProvider(new Mapper());
            var wellProvider = new Well141DataAdapter(dbProvider);
            dbProvider.ResetConnection(string.Empty);
            var well = new Well { Name = "Well-to-add-01", Uid = Uid() };
            var caught = false;
            try
            {
                var result = wellProvider.Add(well);
            }
            catch (WitsmlException)
            {
                caught = true;
            }

            Assert.IsTrue(caught);
        }

        private string Uid()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
