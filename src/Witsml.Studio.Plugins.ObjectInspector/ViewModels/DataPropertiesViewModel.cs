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
using Caliburn.Micro;
using PDS.Framework;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Plugins.ObjectInspector.Models;

namespace PDS.Witsml.Studio.Plugins.ObjectInspector.ViewModels
{
    /// <summary>
    /// Manages the UI behavior for the data properties of an Energistics Data Object.
    /// </summary>
    /// <seealso cref="Screen" />
    public sealed class DataPropertiesViewModel : Screen
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DataPropertiesViewModel));

        private DataObject _dataObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPropertiesViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <exception cref="ArgumentNullException"><paramref name="runtime"/> is null.</exception>
        public DataPropertiesViewModel(IRuntimeService runtime)
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
        /// All (nested) data properties of the Energistics Data Object
        /// </summary>
        public IReadOnlyCollection<DataProperty> DataProperties => DataObject?.NestedDataProperties;
         
        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }
    }
}
