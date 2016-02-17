using Caliburn.Micro;
using PDS.Witsml.Studio.Models;

namespace PDS.Witsml.Studio.ViewModels
{
    public class ConnectionViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ConnectionViewModel));

        public ConnectionViewModel()
        {
            _log.Debug("Creating View Model");

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
