using System;
using System.Linq;
using System.Runtime.Serialization;
using Caliburn.Micro;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Protocol.Core;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Store user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class StoreViewModel : Screen, ISessionAware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoreViewModel"/> class.
        /// </summary>
        public StoreViewModel(IRuntimeService runtime)
        {
            DisplayName = string.Format("{0:D} - {0}", Protocols.Store);
            Data = new TextEditorViewModel(runtime, "XML");
        }

        /// <summary>
        /// Gets or Sets the Parent <see cref="T:Caliburn.Micro.IConductor" />
        /// </summary>
        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public EtpSettings Model
        {
            get { return Parent.Model; }
        }

        private TextEditorViewModel _data;

        /// <summary>
        /// Gets or sets the data editor.
        /// </summary>
        /// <value>The text editor view model.</value>
        public TextEditorViewModel Data
        {
            get { return _data; }
            set
            {
                if (!string.Equals(_data, value))
                {
                    _data = value;
                    NotifyOfPropertyChange(() => Data);
                }
            }
        }

        private bool _canExecute;

        /// <summary>
        /// Gets or sets a value indicating whether the Store protocol messages can be executed.
        /// </summary>
        /// <value><c>true</c> if Store protocol messages can be executed; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool CanExecute
        {
            get { return _canExecute; }
            set
            {
                if (_canExecute != value)
                {
                    _canExecute = value;
                    NotifyOfPropertyChange(() => CanExecute);
                }
            }
        }

        /// <summary>
        /// Generates a new UUID value.
        /// </summary>
        public void NewUuid()
        {
            Model.Store.Uuid = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets the specified resource's details using the Store protocol.
        /// </summary>
        public void GetObject()
        {
            if (!string.IsNullOrWhiteSpace(Model.Store.Uri))
            {
                Parent.SendGetObject(Model.Store.Uri);
            }
        }

        /// <summary>
        /// Submits the specified resource's details using the Store protocol.
        /// </summary>
        public void PutObject()
        {
            Parent.SendPutObject(Data.Document.Text);
        }

        /// <summary>
        /// Deletes the specified resource using the Store protocol.
        /// </summary>
        public void DeleteObject()
        {
            if (!string.IsNullOrWhiteSpace(Model.Store.Uri))
            {
                Parent.SendDeleteObject(Model.Store.Uri);
            }
        }

        /// <summary>
        /// Called when the <see cref="OpenSession" /> message is recieved.
        /// </summary>
        /// <param name="e">The <see cref="ProtocolEventArgs{OpenSession}" /> instance containing the event data.</param>
        public void OnSessionOpened(ProtocolEventArgs<OpenSession> e)
        {
            if (!e.Message.SupportedProtocols.Any(x => x.Protocol == (int)Protocols.Store))
                return;

            CanExecute = true;
        }

        /// <summary>
        /// Called when the <see cref="Energistics.EtpClient" /> web socket is closed.
        /// </summary>
        public void OnSocketClosed()
        {
            CanExecute = false;
        }
    }
}
