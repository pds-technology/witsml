using Caliburn.Micro;
using Energistics.Datatypes;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the Store user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class StoreViewModel : Screen
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
            //Parent.SendPutObject();
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
    }
}
