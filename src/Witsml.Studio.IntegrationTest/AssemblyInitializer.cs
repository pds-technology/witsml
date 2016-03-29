using System.IO;
using log4net.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Studio
{
    [TestClass]
    public class AssemblyInitializer
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
        }
    }
}
