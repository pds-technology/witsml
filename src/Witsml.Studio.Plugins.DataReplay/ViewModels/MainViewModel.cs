using System;
using System.IO;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Win32;
using Newtonsoft.Json;
using PDS.Witsml.Studio.Plugins.DataReplay.Properties;
using PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Simulation;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IPluginViewModel
    {
        public MainViewModel()
        {
            DisplayName = Settings.Default.PluginDisplayName;
        }

        /// <summary>
        /// The ascending display order of a plugin tab
        /// </summary>
        public int DisplayOrder
        {
            get { return Settings.Default.PluginDisplayOrder; }
        }

        public void NewDataReplay()
        {
            var viewModel = new DataReplayViewModel()
            {
                DisplayName = string.Format("Data Replay {0:yyMMdd-HHmmss}", DateTime.Now)
            };

            ActivateItem(viewModel);
        }

        public void NewSimulation()
        {
            var viewModel = new SimulationViewModel()
            {
                DisplayName = string.Format("Simulation {0:yyMMdd-HHmmss}", DateTime.Now)
            };

            ActivateItem(viewModel);
        }

        public void OpenDataReplay()
        {
        }

        public void OpenSimulation()
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Open Simulation Configuration Settings file...",
                Filter = "JSON Files|*.json;*.js|All Files|*.*"
            };

            if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var model = JsonConvert.DeserializeObject<Models.Simulation>(json);

                    var viewModel = new SimulationViewModel()
                    {
                        Model = model,
                        DisplayName = model.Name
                    };

                    ActivateItem(viewModel);
                }
                catch (Exception ex)
                {
                    Application.Current.ShowError("Error opening file.", ex);
                }
            }
        }

        public void DeleteItem()
        {
            if (ActiveItem != null)
            {
                this.CloseItem(ActiveItem);
            }
        }
    }
}
