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

using System;
using MongoDB.Bson.Serialization;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Generates globally unique identifier values as strings.
    /// </summary>
    /// <seealso cref="MongoDB.Bson.Serialization.IIdGenerator" />
    public class UidGenerator : IIdGenerator
    {
        private static readonly string EmptyUid = Guid.Empty.ToString();
        private static readonly Lazy<UidGenerator> _generator;

        /// <summary>
        /// Initializes the <see cref="UidGenerator"/> class.
        /// </summary>
        static UidGenerator()
        {
            _generator = new Lazy<UidGenerator>();
        }

        /// <summary>
        /// Gets the singleton instance of the UidGenerator.
        /// </summary>
        /// <value>The singleton instance.</value>
        public static UidGenerator Instance
        {
            get { return _generator.Value; }
        }

        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver).</param>
        /// <param name="document">The document.</param>
        /// <returns>A globally unique identifier.</returns>
        public object GenerateId(object container, object document)
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            var uid = String.Format("{0}", id);
            return String.IsNullOrWhiteSpace(uid) || EmptyUid.Equals(uid);
        }
    }
}
