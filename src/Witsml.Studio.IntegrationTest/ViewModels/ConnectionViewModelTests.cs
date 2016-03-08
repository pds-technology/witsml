using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Studio.ViewModels
{
    [TestClass]
    public class ConnectionViewModelTests : ConnectionViewModelTestBase
    {
        [Ignore]
        [TestMethod]
        public async Task TestAcceptWithDataItem()
        {
            var newName = "xxx";

            // Initialze the Edit Item by setting the DataItem
            _witsmlConnectionVm.DataItem = _witsmlConnection;
            _witsmlConnectionVm.InitializeEditItem();

            // Make a change to the edit item
            _witsmlConnectionVm.EditItem.Name = newName;

            // Accept the changes
            _witsmlConnectionVm.Accept();

            Assert.AreEqual(newName, _witsmlConnectionVm.DataItem.Name);

            await Task.Yield();
        }
    }
}
