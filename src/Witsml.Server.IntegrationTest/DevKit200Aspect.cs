using Energistics.DataAccess;

namespace PDS.Witsml.Server
{
    public class DevKit200Aspect : DevKitAspect
    {
        public DevKit200Aspect() : base(null, WMLSVersion.WITSML141)
        {
        }

        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version200.Value; }
        }
    }
}
