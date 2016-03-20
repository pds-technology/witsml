using Energistics.Common;
using Energistics.Protocol.Core;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Defines methods that can be implemented to receive <see cref="Energistics.EtpClient"/> status notifications.
    /// </summary>
    public interface ISessionAware
    {
        /// <summary>
        /// Called when the <see cref="OpenSession"/> message is recieved.
        /// </summary>
        /// <param name="e">The <see cref="ProtocolEventArgs{OpenSession}"/> instance containing the event data.</param>
        void OnSessionOpened(ProtocolEventArgs<OpenSession> e);

        /// <summary>
        /// Called when the <see cref="Energistics.EtpClient"/> web socket is closed.
        /// </summary>
        void OnSocketClosed();
    }
}
