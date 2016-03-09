using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Studio.ViewModels
{
    [TestClass]
    public class ConnectionViewModelTests : ConnectionViewModelTestBase
    {
        [TestMethod]
        public void Can_accept_connection_changes_with_dataItem()
        {
            var newName = "xxx";

            // Initialze the Edit Item by setting the DataItem
            _witsmlConnectionVm.DataItem = _witsmlConnection;
            _witsmlConnectionVm.InitializeEditItem();

            // Make a change to the edit item
            _witsmlConnectionVm.EditItem.Name = newName;

            // Accept the changes
            _witsmlConnectionVm.AcceptConnectionChanges();

            Assert.AreEqual(newName, _witsmlConnectionVm.DataItem.Name);
        }
    }
}
