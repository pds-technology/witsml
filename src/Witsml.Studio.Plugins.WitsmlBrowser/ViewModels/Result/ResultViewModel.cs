using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    public class ResultViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public Models.Browser Model
        {
            get { return ((MainViewModel)Parent).Model; }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            ActivateItem(new ResponseViewModel());
            Items.Add(new PropertyViewModel());
            Items.Add(new RawViewModel());
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
