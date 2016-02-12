using System;
using System.Collections.Generic;
using System.Security;
using Energistics.DataAccess;

namespace PDS.Witsml.Client.Linq
{
    public abstract class WitsmlContext : IDisposable
    {
        public WitsmlContext(string url, double timeoutInMinutes, WMLSVersion version)
        {
            Connect(url, timeoutInMinutes, version);
        }

        public WitsmlContext(string url, string username, string password, double timeoutInMinutes, WMLSVersion version)
        {
            Connect(url, username, password, timeoutInMinutes, version);
        }

        public WitsmlContext(string url, string username, SecureString password, double timeoutInMinutes, WMLSVersion version)
        {
            Connect(url, username, password, timeoutInMinutes, version);
        }

        public WITSMLWebServiceConnection Connection { get; private set; }

        public abstract string DataSchemaVersion { get; }

        public List<T> One<T>()
        {
            return new List<T>()
            {
                Activator.CreateInstance<T>()
            };
        }

        protected IWitsmlQuery<T> CreateQuery<T, V>() where V : IEnergisticsCollection
        {
            return new WitsmlQuery<T, V>(this);
        }

        private void Connect(string url, string username, string password, double timeoutInMinutes, WMLSVersion version)
        {
            Connect(url, timeoutInMinutes, version);

            Connection.UseDefaultNetworkCredentials = false;
            Connection.Username = username;
            Connection.SetPassword(password);
        }

        private void Connect(string url, string username, SecureString password, double timeoutInMinutes, WMLSVersion version)
        {
            Connect(url, timeoutInMinutes, version);

            Connection.UseDefaultNetworkCredentials = false;
            Connection.Username = username;
            Connection.SetSecurePassword(password);
        }

        private void Connect(string url, double timeoutInMinutes, WMLSVersion version)
        {
            Connection = new WITSMLWebServiceConnection(url, version);
            Connection.UseDefaultNetworkCredentials = true;
            Connection.Timeout = (int)(60000 * timeoutInMinutes);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.
                Connection = null;

                disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WitsmlContext() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
