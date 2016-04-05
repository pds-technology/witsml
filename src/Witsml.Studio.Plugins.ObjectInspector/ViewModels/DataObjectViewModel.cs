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
using Caliburn.Micro;
using Energistics.DataAccess.Reflection;
using PDS.Framework;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Plugins.ObjectInspector.Models;

namespace PDS.Witsml.Studio.Plugins.ObjectInspector.ViewModels
{
    /// <summary>
    /// Manages the UI behavior for an Energistics Data Object.
    /// </summary>
    /// <seealso cref="Screen" />
    public sealed class DataObjectViewModel : Screen
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DataObjectViewModel));

        private DataObject _dataObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <exception cref="ArgumentNullException"><paramref name="runtime"/> is null.</exception>
        public DataObjectViewModel(IRuntimeService runtime)
        {
            runtime.NotNull(nameof(runtime));

            Log.Debug("Creating view model instance");
            Runtime = runtime;
        }

        /// <summary>
        /// Gets or sets the family version of the objects to display.
        /// </summary>
        public DataObject DataObject
        {
            get {  return _dataObject; }
            set
            {
                if (_dataObject == value) return;

                _dataObject = value;

                Refresh();
            }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// The Energistics Data Object's name.
        /// </summary>
        public string Name => DataObject?.Name;

        /// <summary>
        /// The Energistics Data Object's XML type.
        /// </summary>
        public string XmlType => DataObject?.XmlType;

        /// <summary>
        /// The Energistic Data Object's standard family.
        /// </summary>
        public StandardFamily? StandardFamily => DataObject?.StandardFamily;

        /// <summary>
        /// The Energistic Data Object's data schema version.
        /// </summary>
        public Version DataSchemaVersion => DataObject?.DataSchemaVersion;

        /// <summary>
        /// The Energistics Data Object's description.
        /// </summary>
        public string Description => DataObject?.Description;
    }
}
