//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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

namespace PDS.WITSMLstudio.Store
{

    /// <summary>
    /// Witsml result class
    /// </summary>
    public class WitsmlResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlResult"/> class.
        /// </summary>
        /// <param name="code">The code.</param>
        public WitsmlResult(ErrorCodes code) : this(code, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlResult"/> class.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="message">The message.</param>
        public WitsmlResult(ErrorCodes code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>
        /// Gets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        public ErrorCodes Code { get; private set; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; private set; }
    }

    /// <summary>
    /// Typed Witsml result class 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WitsmlResult<T> : WitsmlResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlResult{T}"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="results">The results.</param>
        public WitsmlResult(ErrorCodes errorCode, T results) : this(errorCode, string.Empty, results)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlResult{T}"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        /// <param name="results">The results.</param>
        public WitsmlResult(ErrorCodes errorCode, string message, T results) : base(errorCode, message)
        {
            Results = results;
        }

        /// <summary>
        /// Gets the results.
        /// </summary>
        /// <value>
        /// The results.
        /// </value>
        public T Results { get; private set; }
    }
}
