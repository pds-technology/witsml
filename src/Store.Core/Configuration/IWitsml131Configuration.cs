//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Defines a method that can be used to supply server capabilities for WITSML API version 1.3.1.
    /// </summary>
    public interface IWitsml131Configuration
    {
        /// <summary>
        /// Gets the server capabilities.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        void GetCapabilities(CapServer capServer);
    }
}
