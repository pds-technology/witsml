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

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Helper methods for <see cref="Functions"/>.
    /// </summary>
    public static class FunctionExtensions
    {
        /// <summary>
        /// Indicates whether this function requires object type as an input.
        /// </summary>
        /// <param name="function">The function to check.</param>
        /// <returns><c>true</c> if the function requires object type; <c>false</c> otherwise.</returns>
        public static bool RequiresObjectType(this Functions function)
        {
            switch (function)
            {
                case Functions.GetFromStore:
                case Functions.UpdateInStore:
                case Functions.AddToStore:
                case Functions.DeleteFromStore:
                    return true;

                default: return false;
            }
        }

        /// <summary>
        /// Indicates whether this function supports compressing the client request.
        /// </summary>
        /// <param name="function">The function to check.</param>
        /// <returns><c>true</c> if the function supports compressing the client request; <c>false</c> otherwise.</returns>
        public static bool SupportsRequestCompression(this Functions function)
        {
            switch (function)
            {
                case Functions.GetFromStore:
                case Functions.UpdateInStore:
                case Functions.AddToStore:
                    return true;

                default: return false;
            }
        }
    }
}
