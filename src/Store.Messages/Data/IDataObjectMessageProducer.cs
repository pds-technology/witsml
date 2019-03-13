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

using System.Threading.Tasks;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Defines a method to send data object messages.
    /// </summary>
    public interface IDataObjectMessageProducer
    {
        /// <summary>
        /// Sends the message asynchronously.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="key">The key.</param>
        /// <param name="payload">The payload.</param>
        /// <returns>An awaitable task.</returns>
        Task SendMessageAsync(string topic, string key, string payload);
    }
}