using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Win32;
using Newtonsoft.Json;
using PDS.Witsml.Studio.Plugins.DataReplay.Properties;
using PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Simulation;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IPluginViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        [ImportingConstructor]
        public MainViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = Settings.Default.PluginDisplayName;
        }

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder
        {
            get { return Settings.Default.PluginDisplayOrder; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; private set; }

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
            var viewModel = new SimulationViewModel(Runtime)
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

                    var viewModel = new SimulationViewModel(Runtime)
                    {
                        Model = model,
                        DisplayName = model.Name
                    };

                    ActivateItem(viewModel);
                }
                catch (Exception ex)
                {
                    Runtime.ShowError("Error opening file.", ex);
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
