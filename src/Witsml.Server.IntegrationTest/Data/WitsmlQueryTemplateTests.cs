using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class WitsmlQueryTemplateTests
    {
        [TestMethod]
        public void WitsmlQueryTemplate_creates_a_full_131_well_template()
        {
            var template = new WitsmlQueryTemplate<Witsml131.Well>();
            var xml = template.AsXml<Witsml131.WellList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<statusWell>abandoned</statusWell>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_creates_a_full_131_wellbore_template()
        {
            var template = new WitsmlQueryTemplate<Witsml131.Wellbore>();
            var xml = template.AsXml<Witsml131.WellboreList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<statusWellbore>abandoned</statusWellbore>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_creates_a_full_131_log_template()
        {
            var template = new WitsmlQueryTemplate<Witsml131.Log>();
            var xml = template.AsXml<Witsml131.LogList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<direction>decreasing</direction>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_creates_a_full_141_well_template()
        {
            var template = new WitsmlQueryTemplate<Witsml141.Well>();
            var xml = template.AsXml<Witsml141.WellList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<statusWell>abandoned</statusWell>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_creates_a_full_141_wellbore_template()
        {
            var template = new WitsmlQueryTemplate<Witsml141.Wellbore>();
            var xml = template.AsXml<Witsml141.WellboreList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<statusWellbore>abandoned</statusWellbore>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_creates_a_full_141_log_template()
        {
            var template = new WitsmlQueryTemplate<Witsml141.Log>();
            var xml = template.AsXml<Witsml141.LogList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<direction>decreasing</direction>"));
        }
    }
}
