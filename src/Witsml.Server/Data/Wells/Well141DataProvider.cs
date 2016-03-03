using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Server.Data.Wells
{
    [Export141(ObjectTypes.Well, typeof(IWitsmlDataProvider))]
    [Export141(ObjectTypes.Well, typeof(IWitsmlDataWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well141DataProvider : WitsmlDataProvider<WellList, Well>
    {
        [ImportingConstructor]
        public Well141DataProvider(IWitsmlDataAdapter<Well> dataAdapter) : base(dataAdapter)
        {
        }
    }
}
