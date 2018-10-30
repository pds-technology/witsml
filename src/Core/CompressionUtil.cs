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

using Energistics.DataAccess;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Class to help with client request compression.
    /// </summary>
    public static class CompressionUtil
    {
        /// <summary>
        /// Decompresses the input request
        /// </summary>
        /// <param name="request">The request to decompress.</param>
        /// <returns>The decompressed request.</returns>
        /// <exception cref="WitsmlException">If the request cannot be decompressed.</exception>
        public static string DecompressRequest(string request)
        {
            try
            {
                return ClientCompression.Base64DecodeAndGZipDecompress(request);
            }
            catch { throw new WitsmlException(ErrorCodes.CannotDecompressQuery); }
        }
    }
}
