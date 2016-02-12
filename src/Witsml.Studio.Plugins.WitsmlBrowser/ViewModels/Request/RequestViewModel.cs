using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class RequestViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public Models.Browser Model
        {
            get { return ((MainViewModel)Parent).Model; }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            ActivateItem(new SettingsViewModel());
            Items.Add(new TreeViewViewModel());
            Items.Add(new TemplatesViewModel());
            Items.Add(new QueryViewModel());
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                foreach (var child in Items.ToArray())
                {
                    this.CloseItem(child);
                }
            }

            base.OnDeactivate(close);
        }
    }
}
