//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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
using System.ServiceModel;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using Energistics.DataAccess.Validation;
using log4net;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Transactions;

namespace PDS.Witsml.Server
{
    /// <summary>
    /// Provides contextual properties of WITSML requests and responses.
    /// </summary>
    /// <seealso cref="System.ServiceModel.IExtension{OperationContext}" />
    public class WitsmlOperationContext : IExtension<OperationContext>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlOperationContext));

        [ThreadStatic]
        private static WitsmlOperationContext _current;

        /// <summary>
        /// Prevents a default instance of the <see cref="WitsmlOperationContext"/> class from being created.
        /// </summary>
        private WitsmlOperationContext()
        {
            _log.Debug("Instance created.");
            Warnings = new List<WitsmlValidationResult>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlOperationContext"/> class.
        /// </summary>
        /// <param name="operationContext">The operation context.</param>
        private WitsmlOperationContext(OperationContext operationContext) : this()
        {
            operationContext.Extensions.Add(this);
        }

        /// <summary>
        /// Gets the current <see cref="WitsmlOperationContext"/>.
        /// </summary>
        /// <value>The current context.</value>
        public static WitsmlOperationContext Current
        {
            get
            {
                if (OperationContext.Current == null)
                {
                    return _current ?? (_current = new WitsmlOperationContext());
                }

                return OperationContext.Current.Extensions.Find<WitsmlOperationContext>()
                       ?? new WitsmlOperationContext(OperationContext.Current);
            }
            internal set
            {
                if (OperationContext.Current == null)
                    _current = value;
            }
        }

        /// <summary>
        /// Gets the name of the current user.
        /// </summary>
        /// <value>The current user's name.</value>
        public string User
        {
            get
            {
                var user = HttpContext.Current?.User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(user))
                    user = Thread.CurrentPrincipal?.Identity?.Name;

                return string.IsNullOrWhiteSpace(user) ? "unknown" : user;
            }
        }

        /// <summary>
        /// Gets or sets the transaction.
        /// </summary>
        /// <value>The transaction.</value>
        public IWitsmlTransaction Transaction { get; set; }

        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public RequestContext Request { get; set; }

        /// <summary>
        /// Gets or sets the response context.
        /// </summary>
        /// <value>The response context.</value>
        public ResponseContext Response { get; set; }

        /// <summary>
        /// Gets or sets the parsed XML document.
        /// </summary>
        /// <value>The XML document.</value>
        public XDocument Document { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is cascade delete.
        /// </summary>
        /// <value><c>true</c> if requesting cascade delete; otherwise, <c>false</c>.</value>
        public bool IsCascadeDelete { get; set; }

        /// <summary>
        /// Gets the list of validation warnings encountered during the operation.
        /// </summary>
        /// <value>The list of validation warnings.</value>
        public List<WitsmlValidationResult> Warnings { get; }

        /// <summary>
        /// Enables an extension object to find out when it has been aggregated. Called when the extension is
        /// added to the <see cref="P:System.ServiceModel.IExtensibleObject`1.Extensions" /> property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Attach(OperationContext owner)
        {
        }

        /// <summary>
        /// Enables an object to find out when it is no longer aggregated. Called when an extension is
        /// removed from the <see cref="P:System.ServiceModel.IExtensibleObject`1.Extensions" /> property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Detach(OperationContext owner)
        {
        }
    }
}
