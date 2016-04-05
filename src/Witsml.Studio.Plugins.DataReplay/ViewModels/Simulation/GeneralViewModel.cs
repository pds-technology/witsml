//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

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
