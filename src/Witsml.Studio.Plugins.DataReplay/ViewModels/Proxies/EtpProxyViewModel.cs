using System;
using System.Threading;
using System.Threading.Tasks;
using Energistics;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Proxies
{
    public abstract class EtpProxyViewModel
    {
        public EtpProxyViewModel(IRuntimeService runtime, Action<string> log)
        {
            Runtime = runtime;
            Log = log;
        }

        public IRuntimeService Runtime { get; private set; }

        public Action<string> Log { get; private set; }

        public Models.Simulation Model { get; protected set; }

        public EtpClient Client { get; protected set; }

        public abstract Task Start(Models.Simulation model, CancellationToken token, int interval = 5000);
    }
}