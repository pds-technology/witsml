using System;
using System.Linq;
using Caliburn.Micro;
using Energistics.DataAccess.WITSML141.ReferenceData;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Simulation
{
    public class GeneralViewModel : Screen
    {
        public GeneralViewModel()
        {
            DisplayName = "General";
            LogIndexTypes = new BindableCollection<LogIndexType>();
            LogIndexTypes.AddRange(Enum.GetValues(typeof(LogIndexType)).OfType<LogIndexType>());
        }

        public Models.Simulation Model
        {
            get { return ((SimulationViewModel)Parent).Model; }
        }

        public BindableCollection<LogIndexType> LogIndexTypes { get; }

        public void NewWellUid()
        {
            Model.WellUid = Guid.NewGuid().ToString();
        }

        public void NewWellboreUid()
        {
            Model.WellboreUid = Guid.NewGuid().ToString();
        }

        public void NewLogUid()
        {
            Model.LogUid = Guid.NewGuid().ToString();
        }

        public void Save()
        {
            ((SimulationViewModel)Parent).Save();
        }
    }
}
