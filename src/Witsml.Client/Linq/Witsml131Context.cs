using System.Security;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Client.Linq
{
    public class Witsml131Context : WitsmlContext
    {
        public Witsml131Context(string url, double timeoutInMinutes = 1.5)
            : base(url, timeoutInMinutes, WMLSVersion.WITSML131)
        {
        }

        public Witsml131Context(string url, string username, string password, double timeoutInMinutes = 1.5)
            : base(url, username, password, timeoutInMinutes, WMLSVersion.WITSML131)
        {
        }

        public Witsml131Context(string url, string username, SecureString password, double timeoutInMinutes = 1.5)
            : base(url, username, password, timeoutInMinutes, WMLSVersion.WITSML131)
        {
        }

        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version131.Value; }
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
