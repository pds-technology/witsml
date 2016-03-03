using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Wells
{
    [Export131(ObjectTypes.Well, typeof(IWitsmlDataProvider))]
    [Export131(ObjectTypes.Well, typeof(IWitsmlDataWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well131DataProvider : WitsmlDataProvider<WellList, Well>
    {
        [ImportingConstructor]
        public Well131DataProvider(IWitsmlDataAdapter<Well> dataAdapter) : base(dataAdapter)
        {
        }
    }
}
