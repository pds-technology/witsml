using Caliburn.Micro;
using ICSharpCode.AvalonEdit.Document;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;

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
        public StoreViewModel()
        {
            DisplayName = "Store";
            Data = new TextDocument();
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

        private TextDocument _data;
        /// <summary>
        /// Gets or sets the data document.
        /// </summary>
        /// <value>The output.</value>
        public TextDocument Data
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
            Parent.SendGetObject(Model.Store.Uri);
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
            Parent.SendDeleteObject(Model.Store.Uri);
        }
    }
}
