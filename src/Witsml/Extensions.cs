using Energistics.DataAccess;
using PDS.Witsml.Properties;

namespace PDS.Witsml
{
    public static class Extensions
    {
        public static string GetDescription(this ErrorCodes errorCode)
        {
            return Resources.ResourceManager.GetString(errorCode.ToString(), Resources.Culture);
        }

        public static string GetVersion<T>(this T dataObject) where T : IEnergisticsCollection
        {
            return (string)dataObject.GetType().GetProperty("Version").GetValue(dataObject, null);
        }

        public static T SetVersion<T>(this T dataObject, string version) where T : IEnergisticsCollection
        {
            dataObject.GetType().GetProperty("Version").SetValue(dataObject, version);
            return dataObject;
        }
    }
}
