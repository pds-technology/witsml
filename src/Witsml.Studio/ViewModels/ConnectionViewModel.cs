using Caliburn.Micro;
using PDS.Witsml.Studio.Models;

namespace PDS.Witsml.Studio.ViewModels
{
    public class ConnectionViewModel : Screen
    {
        public ConnectionViewModel()
        {
            DisplayName = "Connection";
            Connection = new Connection()
            {
                //Uri = "http://localhost:5000",
                //Name = "Name",
                //Username = "username",
                //Password = "password"
            };
        }

        public Connection Connection { get; set; }
    }
}
