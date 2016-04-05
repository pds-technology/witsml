//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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

namespace PDS.Witsml
{
    /// <summary>
    /// Represents errors that occur during WITSML Store API method execution.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class WitsmlException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlException" /> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        public WitsmlException(ErrorCodes errorCode) : this(errorCode, errorCode.GetDescription())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        public WitsmlException(ErrorCodes errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public WitsmlException(ErrorCodes errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="innerException">The inner exception.</param>
        public WitsmlException(ErrorCodes errorCode, Exception innerException) : base(errorCode.GetDescription(), innerException)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Gets the WITSML error code.
        /// </summary>
        /// <value>The error code.</value>
        public ErrorCodes ErrorCode { get; private set; }
    }
}
