using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.Connections;
using PDS.Witsml.Studio.Properties;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Base class for testing the ConnectionViewModel
    /// </summary>
    [TestClass]
    public class ConnectionViewModelTestBase
    {
        protected static readonly string PersistedDataFolderName = Settings.Default.PersistedDataFolderName;
        protected static readonly string ConnectionBaseFileName = Settings.Default.ConnectionBaseFileName;

        protected BootstrapperHarness _bootstrapper;
        protected TestRuntimeService _runtime;

        protected ConnectionViewModel _witsmlConnectionVm;
        protected ConnectionViewModel _etpConnectionVm;
        protected Connection _witsmlConnection;
        protected Connection _etpConnection;

        /// <summary>
        /// Sets up the environment for each test.  
        /// ConnectionViewModels and Connections are created 
        /// for ConnectionTypes Witsml and Etp.
        /// In addition the persisence folder is cleard and deleted.
        /// </summary>
        [TestInitialize]
        public void TestSetUp()
        {
            _bootstrapper = new BootstrapperHarness();
            _runtime = new TestRuntimeService(_bootstrapper.Container);

            _witsmlConnection = new Connection()
            {
                Name = "Witsml",
                Uri = "http://localhost/Witsml.Web/WitsmlStore.svc",
                Username = "WitsmlUser"
            };

            _etpConnection = new Connection()
            {
                Name = "Etp",
                Uri = "ws://localhost/witsml.web/api/etp",
                Username = "EtpUser"
            };

            _witsmlConnectionVm = new ConnectionViewModel(_runtime, ConnectionTypes.Witsml);
            _etpConnectionVm = new ConnectionViewModel(_runtime, ConnectionTypes.Etp);

            DeletePersistenceFolder();
        }

        protected static void DeletePersistenceFolder()
        {
            var path = string.Format("{0}/{1}", Environment.CurrentDirectory, PersistedDataFolderName);

            // Delete the Persistence Folder
            if (Directory.Exists(path))
            {
                // Delete all files in the Persistence Folder
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                Directory.Delete(path);
            }
        }
    }
}
