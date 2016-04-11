//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Caliburn.Micro;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Core;
using Energistics.Protocol.Store;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;

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
            SendPutObject(Data.Document.Text);
        }

        /// <summary>
        /// Sends the <see cref="Energistics.Protocol.Store.PutObject"/> message with the supplied XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        public void SendPutObject(string xml)
        {
            var uri = new EtpUri(Model.Store.Uri);

            var dataObject = new DataObject()
            {
                Resource = new Resource()
                {
                    Uri = uri,
                    Uuid = Model.Store.Uuid,
                    Name = Model.Store.Name,
                    HasChildren = -1,
                    ContentType = uri.ContentType,
                    ResourceType = ResourceTypes.DataObject.ToString(),
                    CustomData = new Dictionary<string, string>()
                }
            };

            dataObject.SetXml(xml);

            Parent.Client.Handler<IStoreCustomer>()
                .PutObject(dataObject);
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
