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

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.Models
{
    /// <summary>
    /// Encapsulates the input and output parameters passed to the WITSML Store API methods.
    /// </summary>
    public struct WitsmlResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlResult" /> struct.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="xmlIn">The XML in.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="capClientIn">The cap client in.</param>
        /// <param name="xmlOut">The XML out.</param>
        /// <param name="messageOut">The message out.</param>
        /// <param name="returnCode">The return code.</param>
        public WitsmlResult(string objectType, string xmlIn, string optionsIn, string capClientIn, string xmlOut, string messageOut, short returnCode)
        {
            ObjectType = objectType;
            XmlIn = xmlIn;
            OptionsIn = optionsIn;
            CapClientIn = capClientIn;
            XmlOut = xmlOut;
            MessageOut = messageOut;
            ReturnCode = returnCode;
        }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <value>The type of the object.</value>
        public string ObjectType { get; private set; }

        /// <summary>
        /// Gets the XML in.
        /// </summary>
        /// <value>The XML in.</value>
        public string XmlIn { get; private set; }

        /// <summary>
        /// Gets the options in.
        /// </summary>
        /// <value>The options in.</value>
        public string OptionsIn { get; private set; }

        /// <summary>
        /// Gets the cap client in.
        /// </summary>
        /// <value>The cap client in.</value>
        public string CapClientIn { get; private set; }

        /// <summary>
        /// Gets the XML out.
        /// </summary>
        /// <value>The XML out.</value>
        public string XmlOut { get; private set; }

        /// <summary>
        /// Gets the message out.
        /// </summary>
        /// <value>The message out.</value>
        public string MessageOut { get; private set; }

        /// <summary>
        /// Gets the return code.
        /// </summary>
        /// <value>The return code.</value>
        public short ReturnCode { get; private set; }
    }
}
