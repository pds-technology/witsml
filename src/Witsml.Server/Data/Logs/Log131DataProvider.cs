using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Logs
{
    [Export131(ObjectTypes.Log, typeof(IWitsmlDataProvider))]
    [Export131(ObjectTypes.Log, typeof(IWitsmlDataWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log131DataProvider : WitsmlDataProvider<LogList, Log>
    {
        [ImportingConstructor]
        public Log131DataProvider(IWitsmlDataAdapter<Log> dataAdapter) : base(dataAdapter)
        {
        }
    }
}
