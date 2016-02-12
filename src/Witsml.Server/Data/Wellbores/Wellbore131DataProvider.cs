using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Wellbores
{
    [Export131(ObjectTypes.Wellbore, typeof(IWitsmlDataProvider))]
    [Export131(ObjectTypes.Wellbore, typeof(IWitsmlDataWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Wellbore131DataProvider : WitsmlDataProvider<WellboreList, Wellbore>
    {
        [ImportingConstructor]
        public Wellbore131DataProvider(IWitsmlDataAdapter<Wellbore> dataAdapter) : base(dataAdapter)
        {
        }

        protected override WitsmlResult<IEnergisticsCollection> FormatResponse(WitsmlQueryParser parser, WitsmlResult<List<Wellbore>> result)
        {
            // TODO: format response according to OptionsIn

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                new WellboreList()
                {
                    Wellbore = result.Results
                });
        }
    }
}
