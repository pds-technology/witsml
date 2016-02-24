using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Wells
{
    [Export131(ObjectTypes.Well, typeof(IWitsmlDataProvider))]
    [Export131(ObjectTypes.Well, typeof(IWitsmlDataWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well131DataProvider : WitsmlDataProvider<WellList, Well>
    {
        [ImportingConstructor]
        public Well131DataProvider(IWitsmlDataAdapter<Well> dataAdapter) : base(dataAdapter, OptionsIn.DataVersion.Version131.Value)
        {
        }

        protected override WitsmlResult<IEnergisticsCollection> FormatResponse(WitsmlQueryParser parser, WitsmlResult<List<Well>> result)
        {
            // TODO: format response according to OptionsIn

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                new WellList()
                {
                    Well = result.Results
                });
        }
    }
}
