using Caliburn.Micro;

namespace PDS.Witsml.Studio.ViewModels
{
    public class TestViewModel : Screen, IPluginViewModel
    {
        public TestViewModel()
        {
            DisplayName = DisplayOrder.ToString();
        }

        public int DisplayOrder
        {
            get
            {
                return 100;
            }
        }
    }
}
