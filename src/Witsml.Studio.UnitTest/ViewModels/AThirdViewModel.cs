using Caliburn.Micro;

namespace PDS.Witsml.Studio.ViewModels
{
    public class AThirdViewModel : Screen, IPluginViewModel
    {
        public AThirdViewModel()
        {
            DisplayName = DisplayOrder.ToString();
        }

        public int DisplayOrder
        {
            get
            {
                return 300;
            }
        }
    }
}
