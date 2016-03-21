using System.Linq;
using System.Windows;
using Caliburn.Micro;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Protocol.Core;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the tree view user interface elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class HierarchyViewModel : Screen, ISessionAware
    {
        private static readonly string[] DescribeObjectTypes = new[]
        {
            ObjectTypes.Wellbore, ObjectTypes.Log, ObjectTypes.ChannelSet, ObjectTypes.Channel
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="HierarchyViewModel"/> class.
        /// </summary>
        public HierarchyViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = string.Format("{0:D} - {0}", Protocols.Discovery);
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

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; private set; }

        /// <summary>
        /// Determines whether the GetObject message can be sent for the selected resource.
        /// </summary>
        /// <returns><c>true</c> if the selected resource's level is greater than 1; otherwise, <c>false</c>.</returns>
        public bool CanGetObject
        {
            get
            {
                var resource = Parent.SelectedResource;

                return resource != null && resource.Level > 0 && 
                    !string.IsNullOrWhiteSpace(new EtpUri(resource.Resource.Uri).ObjectId);
            }
        }

        /// <summary>
        /// Gets the selected resource's details using the Store protocol.
        /// </summary>
        public void GetObject()
        {
            Parent.GetObject();
        }

        /// <summary>
        /// Determines whether the DeleteObject message can be sent for the selected resource.
        /// </summary>
        /// <returns><c>true</c> if the selected resource's level is greater than 1; otherwise, <c>false</c>.</returns>
        public bool CanDeleteObject
        {
            get { return CanGetObject; }
        }

        /// <summary>
        /// Deletes the selected resource using the Store protocol.
        /// </summary>
        public void DeleteObject()
        {
            if (Runtime.ShowConfirm("Are you sure you want to delete the selected resource?", MessageBoxButton.YesNo))
            {
                Parent.DeleteObject();
            }
        }

        /// <summary>
        /// Determines whether the ChannelDescribe message can be sent for the selected resource.
        /// </summary>
        /// <value><c>true</c> if the channels can be described; otherwise, <c>false</c>.</value>
        public bool CanDescribeChannels
        {
            get
            {
                var resource = Parent.SelectedResource;

                if (resource != null && resource.Level > 0)
                {
                    var uri = new EtpUri(resource.Resource.Uri);
                    return DescribeObjectTypes.Contains(uri.ObjectType);
                }

                return false;
            }
        }

        /// <summary>
        /// Requests channel metadata for the selected resource using the ChannelStreaming protocol.
        /// </summary>
        public void DescribeChannels()
        {
            Parent.DescribeChannels();
        }

        /// <summary>
        /// Refreshes the hierarchy.
        /// </summary>
        public void RefreshHierarchy()
        {
            Parent.RefreshHierarchy();
        }

        /// <summary>
        /// Refreshes the context menu.
        /// </summary>
        public void RefreshContextMenu()
        {
            NotifyOfPropertyChange(() => CanGetObject);
            NotifyOfPropertyChange(() => CanDeleteObject);
            NotifyOfPropertyChange(() => CanDescribeChannels);
        }

        /// <summary>
        /// Called when the <see cref="OpenSession" /> message is recieved.
        /// </summary>
        /// <param name="e">The <see cref="ProtocolEventArgs{OpenSession}" /> instance containing the event data.</param>
        public void OnSessionOpened(ProtocolEventArgs<OpenSession> e)
        {
            if (!e.Message.SupportedProtocols.Any(x => x.Protocol == (int)Protocols.Discovery))
                return;

            Parent.GetResources(EtpUri.RootUri);
        }

        /// <summary>
        /// Called when the <see cref="Energistics.EtpClient" /> web socket is closed.
        /// </summary>
        public void OnSocketClosed()
        {
        }
    }
}
