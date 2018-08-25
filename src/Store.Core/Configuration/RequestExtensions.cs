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
                xml: request.QueryIn,
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
                xml: request.XMLin,
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
                xml: request.XMLin,
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
                xml: request.QueryIn,
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
    }
}
