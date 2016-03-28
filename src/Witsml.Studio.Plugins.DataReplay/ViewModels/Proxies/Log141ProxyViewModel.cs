using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes.ChannelData;
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Proxies
{
    public class Log141ProxyViewModel : WitsmlProxyViewModel
    {
        public Log141ProxyViewModel(IRuntimeService runtime, Connections.Connection connection) : base(connection, WMLSVersion.WITSML141)
        {
            Runtime = runtime;
            Generator = new Log141Generator();
        }

        public IRuntimeService Runtime { get; private set; }

        public Log141Generator Generator { get; private set; }

        public override async Task Start(Models.Simulation model, CancellationToken token, int interval = 5000)
        {
            var generator = new Log141Generator();
            var index = 0d;

            var logList = new Log()
            {
                UidWell = model.WellUid,
                NameWell = model.WellName,
                UidWellbore = model.WellboreUid,
                NameWellbore = model.WellboreName,
                Uid = model.LogUid,
                Name = model.LogName,
                IndexType = model.LogIndexType
            }
            .AsList();

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var result = Connection.Read(new LogList() { Log = logList }, OptionsIn.ReturnElements.HeaderOnly);

                if (!result.Log.Any())
                {
                    Runtime.Invoke(() => Runtime.ShowError("Log not found."));
                    break;
                }

                var log = result.Log[0];

                if (log.IndexType != LogIndexType.datetime && log.EndIndex != null)
                    index = log.EndIndex.Value;

                log.Direction = LogIndexDirection.increasing;
                log.IndexCurve = model.Channels.Select(x => x.Mnemonic).FirstOrDefault();
                log.LogCurveInfo = model.Channels.Select(ToLogCurveInfo).ToList();

                index = generator.GenerateLogData(log, startIndex: index, interval: 0.1);

                result.Log[0].LogData[0].MnemonicList = generator.Mnemonics(result.Log[0].LogCurveInfo);
                result.Log[0].LogData[0].UnitList = generator.Units(result.Log[0].LogCurveInfo);

                Connection.Update(result);

                await Task.Delay(interval);
            }
        }

        private LogCurveInfo ToLogCurveInfo(ChannelMetadataRecord channel)
        {
            return new LogCurveInfo()
            {
                Mnemonic = new ShortNameStruct(channel.Mnemonic),
                Unit = channel.Uom,
                CurveDescription = channel.Description,
                TypeLogData = LogDataType.@double,
            };
        }
    }
}
