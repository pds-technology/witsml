using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Server.Data.Wells
{
    [Export141(ObjectTypes.Well, typeof(IWitsmlDataProvider))]
    [Export141(ObjectTypes.Well, typeof(IWitsmlDataWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well141DataProvider : WitsmlDataProvider<WellList, Well>
    {
        [ImportingConstructor]
        public Well141DataProvider(IWitsmlDataAdapter<Well> dataAdapter) : base(dataAdapter, OptionsIn.DataVersion.Version141.Value)
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
