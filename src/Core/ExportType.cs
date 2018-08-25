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
using System.ComponentModel.Composition;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Specifies that a type, property, field, or method provides a particular export.
    /// </summary>
    /// <seealso cref="System.ComponentModel.Composition.ExportAttribute" />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class ExportTypeAttribute : ExportAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportTypeAttribute"/> class.
        /// </summary>
        /// <param name="contractNameType">The type to use as the contract name.</param>
        public ExportTypeAttribute(Type contractNameType) : base(contractNameType.FullName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportTypeAttribute"/> class.
        /// </summary>
        /// <param name="contractNameType">The type to use as the contract name.</param>
        /// <param name="contractType">The contract type.</param>
        public ExportTypeAttribute(Type contractNameType, Type contractType) : base(contractNameType.FullName, contractType)
        {
        }
    }
}
