using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{
    public class ShellHarness : ShellViewModel
    {
        private IShellViewModel _testShell;

        public ShellHarness(IShellViewModel newShell)
        {
            //this = newShell;
        }

        public IObservableCollection<IScreen> GetItems()
        {

            return this.Items;
        }
    }
}
