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

using System.Xml.Linq;
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Encapsulates common properties used for parsing WITSML elements.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataObjectNavigationContext" />
    public abstract class WitsmlParserContext : DataObjectNavigationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlParserContext"/> class.
        /// </summary>
        /// <param name="element">The XML element.</param>
        protected WitsmlParserContext(XElement element)
        {
            Element = element;
        }

        /// <summary>
        /// Gets the XML element.
        /// </summary>
        /// <value>The XML element.</value>
        public XElement Element { get; }

        /// <summary>
        /// Gets or sets a value indicating whether NaN elements should be removed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if NaN elements should be removed; otherwise, <c>false</c>.
        /// </value>
        public bool RemoveNaNElements { get; set; }
    }

    /// <summary>
    /// Encapsulates common properties used for parsing WITSML elements for a specific data object type.
    /// </summary>
    /// <typeparam name="T">The type of the data object.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataObjectNavigationContext" />
    public class WitsmlParserContext<T> : WitsmlParserContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlParserContext{T}"/> class.
        /// </summary>
        /// <param name="element">The XML element.</param>
        public WitsmlParserContext(XElement element) : base(element)
        {
            DataObjectType = typeof(T);
        }
    }
}
