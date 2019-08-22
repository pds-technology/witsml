//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Energistics.DataAccess.Validation;

namespace PDS.WITSMLstudio.Data
{
    /// <summary>
    /// Encapsulates common properties used for navigating WITSML documents.
    /// </summary>
    public abstract class DataObjectNavigationContext
    {
        private List<WitsmlValidationResult> _warnings;

        /// <summary>
        /// Gets the WITSML API function.
        /// </summary>
        /// <value>The WITSML API function.</value>
        public Functions Function { get; set; }

        /// <summary>
        /// Gets the type of the data object.
        /// </summary>
        /// <value>The type of the data object.</value>
        public Type DataObjectType { get; set; }

        /// <summary>
        /// Gets or sets the list of ignored element names.
        /// </summary>
        /// <value>The list of ignored element names.</value>
        public List<string> Ignored { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether unknown elements should be ignored.
        /// </summary>
        /// <value>
        ///   <c>true</c> if unknown elements should be ignored; otherwise, <c>false</c>.
        /// </value>
        public bool IgnoreUnknownElements { get; set; }

        /// <summary>
        /// Gets the list of validation warnings encountered during navigation.
        /// </summary>
        /// <value>The list of validation warnings.</value>
        public List<WitsmlValidationResult> Warnings => _warnings ?? (_warnings = new List<WitsmlValidationResult>());
    }
}
