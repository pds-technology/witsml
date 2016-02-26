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
        public Models.WitsmlSettings Model
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
    }
}
