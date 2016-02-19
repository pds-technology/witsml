using System;
using System.Windows;
using Caliburn.Micro;
using PDS.Witsml.Studio.Models;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Manages the data entry for connection details.
    /// </summary>
    public class ConnectionViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ConnectionViewModel));

        /// <summary>
        /// Initializes an instance of the ConnectionViewModel.
        /// </summary>
        public ConnectionViewModel(ConnectionTypes connectionType)
        {
            _log.Debug("Creating View Model");

            // TODO: Remove (Task 4373)
            _connectionTest = TestWitsmlConnection;

            ConnectionType = connectionType;
            DisplayName = string.Format("{0} Connection", ConnectionType.ToString().ToUpper());
            Connection = new Connection();
        }

        /// <summary>
        /// Gets the connection type
        /// </summary>
        public ConnectionTypes ConnectionType { get; private set; }

        /// <summary>
        /// Gets and sets the connection details for a connection
        /// </summary>
        public Connection Connection { get; set; }


        // TODO: Implement and resolve an IConnectionTest for Witsml or Etp 
        //... ConnectionType assign (Task 4373)
        private Func<Connection, bool> _connectionTest;
        //public Func<Connection, bool> ConnectionTest
        //{
        //    get { return _connectionTest; }
        //    set
        //    {
        //        if (!ReferenceEquals(_connectionTest, value))
        //        {
        //            _connectionTest = value;
        //            NotifyOfPropertyChange(() => ConnectionTest);
        //        }
        //    }
        //}

        // TODO: Remove after IConnectionTests are created and resolved for use (Task 4373)
        private bool TestWitsmlConnection(Connection connection)
        {
            return !string.IsNullOrEmpty(connection.Uri);
        }

        public void TestConnection()
        {
            if (_connectionTest(Connection))
            {
                MessageBox.Show("Connection successful", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Connection failed", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
