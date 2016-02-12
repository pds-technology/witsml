using System.Security;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Client.Linq
{
    public class Witsml141Context : WitsmlContext
    {
        public Witsml141Context(string url, double timeoutInMinutes = 1.5)
            : base(url, timeoutInMinutes, WMLSVersion.WITSML141)
        {
        }

        public Witsml141Context(string url, string username, string password, double timeoutInMinutes = 1.5)
            : base(url, username, password, timeoutInMinutes, WMLSVersion.WITSML141)
        {
        }

        public Witsml141Context(string url, string username, SecureString password, double timeoutInMinutes = 1.5)
            : base(url, username, password, timeoutInMinutes, WMLSVersion.WITSML141)
        {
        }

        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version141.Value; }
        }

        public IWitsmlQuery<Well> Wells
        {
            get { return CreateQuery<Well, WellList>(); }
        }

        public IWitsmlQuery<Wellbore> Wellbores
        {
            get { return CreateQuery<Wellbore, WellboreList>(); }
        }

        public IWitsmlQuery<Rig> Rigs
        {
            get { return CreateQuery<Rig, RigList>(); }
        }

        public IWitsmlQuery<Log> Logs
        {
            get { return CreateQuery<Log, LogList>(); }
        }

        public IWitsmlQuery<Trajectory> Trajectories
        {
            get { return CreateQuery<Trajectory, TrajectoryList>(); }
        }
    }
}
