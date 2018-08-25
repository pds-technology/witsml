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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML131;
using Energistics.Etp.Common.Datatypes;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    /// <summary>
    /// Provides validation for <see cref="Well" /> data objects.
    /// </summary>
    public partial class Well131Validator
    {
        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            var uri = DataObject.GetUri();

            // Validate UID does not exist
            if (Context.Function != Functions.PutObject && _wellDataAdapter.Exists(uri))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            }
        }

        /// <summary>
        /// Validates the data object while executing UpdateInStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForUpdate()
        {
            var uri = DataObject.GetUri();
            yield return ValidateObjectExistence(uri);
        }

        /// <summary>
        /// Validates the data object while executing UpdateInStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForDelete()
        {
            var uri = DataObject.GetUri();
            yield return ValidateObjectExistence(uri);

            // Validate that there are no child data-objects if cascading deletes are not invoked.
            if (!Parser.HasElements() && !Parser.CascadedDelete() && _wellboreDataAdapter.Any(uri))
            {
                yield return new ValidationResult(ErrorCodes.NotBottomLevelDataObject.ToString());
            }
        }

        private ValidationResult ValidateObjectExistence(EtpUri uri)
        {

            // Validate that a Uid was provided
            if (string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] {"Uid"});
            }
            // Validate that a well for the Uid exists
            else if (!_wellDataAdapter.Exists(uri))
            {
                return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] {"Uid"});
            }

            return null;
        }
    }
}
