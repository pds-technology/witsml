using Caliburn.Micro;

namespace PDS.Witsml.Studio.ViewModels
{
    public class ASecondViewModel : Screen, IPluginViewModel
    {
        public ASecondViewModel()
        {
            DisplayName = DisplayOrder.ToString();
        }

        public int DisplayOrder
        {
            get
            {
                return 200;
            }
        }
    }
}
