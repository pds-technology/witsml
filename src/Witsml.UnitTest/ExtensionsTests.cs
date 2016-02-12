using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void GetDescription_returns_correct_description_for_error_code()
        {
            const string expected = "Function completed successfully";

            var actual = ErrorCodes.Success.GetDescription();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetDescription_returns_null_for_unset_error_code()
        {
            const string expected = null;

            var actual = ErrorCodes.Unset.GetDescription();

            Assert.AreEqual(expected, actual);
        }
    }
}
