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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Configuration
{
    /// <summary>
    /// Provides extension methods that can be used to process WITSML Store API method input paramters.
    /// </summary>
    public static class RequestExtensions
    {
        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The GetVersion request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_GetVersionRequest request)
        {
            return new RequestContext(
                function: Functions.GetVersion,
                objectType: null,
                xml: null,
                options: null,
                capabilities: null);
        }

        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The GetCap request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_GetCapRequest request)
        {
            return new RequestContext(
                function: Functions.GetCap,
                objectType: null,
                xml: null,
                options: request.OptionsIn,
                capabilities: null);
        }

        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The GetFromStore request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_GetFromStoreRequest request)
        {
            return new RequestContext(
                function: Functions.GetFromStore,
                objectType: request.WMLtypeIn,
                xml: DecompressQueryIn(request.QueryIn, request.OptionsIn),
                options: request.OptionsIn,
                capabilities: request.CapabilitiesIn);
        }

        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The AddToStore request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_AddToStoreRequest request)
        {
            return new RequestContext(
                function: Functions.AddToStore,
                objectType: request.WMLtypeIn,
                xml: DecompressQueryIn(request.XMLin, request.OptionsIn),
                options: request.OptionsIn,
                capabilities: request.CapabilitiesIn);
        }

        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The UpdateInStore request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_UpdateInStoreRequest request)
        {
            return new RequestContext(
                function: Functions.UpdateInStore,
                objectType: request.WMLtypeIn,
                xml: DecompressQueryIn(request.XMLin, request.OptionsIn),
                options: request.OptionsIn,
                capabilities: request.CapabilitiesIn);
        }

        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The DeleteFromStore request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_DeleteFromStoreRequest request)
        {
            return new RequestContext(
                function: Functions.DeleteFromStore,
                objectType: request.WMLtypeIn,
                xml: DecompressQueryIn(request.QueryIn, request.OptionsIn),
                options: request.OptionsIn,
                capabilities: request.CapabilitiesIn);
        }

        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The GetBaseMsg request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_GetBaseMsgRequest request)
        {
            return new RequestContext(
                function: Functions.GetBaseMsg,
                objectType: null,
                xml: null,
                options: null,
                capabilities: null);
        }

        /// <summary>
        /// Decompresses the input XML/Query string based on the compressionMethod OptionsIn parameter (if any)
        /// The request context will end up with the uncompressed XML data for further validation and processing.
        /// </summary>
        /// <param name="requestQueryIn">The input XML/Query for the request</param>
        /// <param name="requestOptionsIn">The OptionsIn dictionary supplied with the request</param>
        /// <returns></returns>
        private static string DecompressQueryIn(string requestQueryIn, string requestOptionsIn)
        {
            Dictionary<string, string> optionsDict = OptionsIn.Parse(requestOptionsIn);

            //if not specified or "none"
            if (!optionsDict.ContainsKey(OptionsIn.CompressionMethod.Keyword) ||
                (optionsDict[OptionsIn.CompressionMethod.Keyword]
                    .EqualsIgnoreCase(OptionsIn.CompressionMethod.None.Value)))
            {
                return requestQueryIn;
            }

            if (optionsDict[OptionsIn.CompressionMethod.Keyword].EqualsIgnoreCase(OptionsIn.CompressionMethod.Gzip.Value))
            {
                using (MemoryStream msIn = new MemoryStream())
                {
                    byte[] data = Convert.FromBase64String(requestQueryIn);
                    msIn.Write(data, 0, data.Length);
                    msIn.Seek(0, SeekOrigin.Begin);

                    using (GZipStream gzs = new GZipStream(msIn, CompressionMode.Decompress))
                    {
                        using (MemoryStream msOut = new MemoryStream())
                        {
                            gzs.CopyTo(msOut);

                            return Encoding.UTF8.GetString(msOut.ToArray());
                        }
                    }
                }
            }
            else
            {
                throw new WitsmlException(ErrorCodes.CompressedInputNonConforming, "Compression method not supported.");
            }
        }
    }

}
