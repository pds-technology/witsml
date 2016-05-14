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

using Caliburn.Micro;
using ICSharpCode.AvalonEdit.Document;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    /// <summary>
    /// Manages the behavior for the query result UI elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class ResultViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ResultViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        /// <param name="queryResults">The query results.</param>
        /// <param name="messages">The WITSML messages.</param>
        /// <param name="soapMessages">The SOAP messages.</param>
        public ResultViewModel(IRuntimeService runtime, TextDocument queryResults, TextDocument messages, TextDocument soapMessages)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;
            ObjectData = new DataGridViewModel(runtime);
            ObjectProperties = new PropertyGridViewModel(runtime, ObjectData);

            QueryResults = new TextEditorViewModel(runtime, "XML", true)
            {
                Document = queryResults
            };
            Messages = new TextEditorViewModel(runtime, "XML", true)
            {
                Document = messages
            };
            SoapMessages = new TextEditorViewModel(runtime, "XML", true)
            {
                Document = soapMessages
            };
        }

        /// <summary>
        /// Gets the Parent <see cref="T:Caliburn.Micro.IConductor" /> for this view model.
        /// </summary>
        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets the data model for this view model.
        /// </summary>
        /// <value>
        /// The WitsmlSettings data model.
        /// </value>
        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets or sets the property grid view model.
        /// </summary>
        /// <value>The property grid view model.</value>
        public PropertyGridViewModel ObjectProperties { get; }

        /// <summary>
        /// Gets or sets the data grid view model.
        /// </summary>
        /// <value>The data grid view model.</value>
        public DataGridViewModel ObjectData { get; set; }

        private TextEditorViewModel _queryResults;

        /// <summary>
        /// Gets or sets the query results editor.
        /// </summary>
        /// <value>The query results editor.</value>
        public TextEditorViewModel QueryResults
        {
            get { return _queryResults; }
            set
            {
                if (!ReferenceEquals(_queryResults, value))
                {
                    _queryResults = value;
                    NotifyOfPropertyChange(() => QueryResults);
                }
            }
        }

        private TextEditorViewModel _messages;

        /// <summary>
        /// Gets or sets the WITSML messages editor.
        /// </summary>
        /// <value>The WITSML messages editor.</value>
        public TextEditorViewModel Messages
        {
            get { return _messages; }
            set
            {
                if (!string.Equals(_messages, value))
                {
                    _messages = value;
                    NotifyOfPropertyChange(() => Messages);
                }
            }
        }

        private TextEditorViewModel _soapMessages;

        /// <summary>
        /// Gets or sets the SOAP messages editor.
        /// </summary>
        /// <value>The SOAP messages editor.</value>
        public TextEditorViewModel SoapMessages
        {
            get { return _soapMessages; }
            set
            {
                if (!string.Equals(_soapMessages, value))
                {
                    _soapMessages = value;
                    NotifyOfPropertyChange(() => SoapMessages);
                }
            }
        }
    }
}
