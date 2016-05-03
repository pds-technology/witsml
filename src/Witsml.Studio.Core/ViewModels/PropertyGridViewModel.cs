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

using System.Linq;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.Witsml.Studio.Core.Models;
using PDS.Witsml.Studio.Core.Runtime;

namespace PDS.Witsml.Studio.Core.ViewModels
{
    public class PropertyGridViewModel : Screen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyGridViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public PropertyGridViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; private set; }

        private object _currentObject;

        /// <summary>
        /// Gets or sets the current object.
        /// </summary>
        /// <value>The current object.</value>
        public object CurrentObject
        {
            get { return _currentObject; }
            set
            {
                if (!ReferenceEquals(_currentObject, value))
                {
                    _currentObject = value;
                    NotifyOfPropertyChange(() => CurrentObject);
                }
            }
        }

        /// <summary>
        /// Sets the current object.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="xml">The XML string.</param>
        /// <param name="version">The WITSML version.</param>
        public void SetCurrentObject(string objectType, string xml, string version)
        {
            var dataType = ObjectTypes.GetObjectGroupType(objectType, version);
            var dataObject = WitsmlParser.Parse(dataType, xml);
            var collection = dataObject as IEnergisticsCollection;

            TypeDecorationManager.Register(dataType);
            CurrentObject = dataObject;

            CurrentObject = collection == null
                ? dataObject
                : collection.Items.Count > 1
                ? collection.Items
                : collection.Items.Count == 1
                ? collection.Items[0]
                : collection;
        }
    }
}
