//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML141;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Datatypes;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Provides validation for <see cref="Wellbore" /> data objects.
    /// </summary>
    public partial class Wellbore141Validator
    {
        /// <summary>
        /// Gets or sets the collection of <see cref="IWitsml141Configuration"/> providers.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IWitsml141Configuration> Providers { get; set; }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            var uri = DataObject.GetUri();
            var uriWell = uri.Parent;
            var well = _wellDataAdapter.Get(uriWell);

            // Validate parent uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell))
            {
                yield return new ValidationResult(ErrorCodes.MissingElementUidForAdd.ToString(), new[] { "UidWell" });
            }

            // Validate parent exists
            else if (well == null)
            {
                yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWell" });
            }

            else if (!well.Uid.Equals(DataObject.UidWell))
            {
                yield return new ValidationResult(ErrorCodes.IncorrectCaseParentUid.ToString(), new[] { "UidWell" });
            }

            // Validate UID does not exist
            else if (Context.Function != Functions.PutObject && _wellboreDataAdapter.Exists(uri))
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
        /// Validates the data object while executing DeleteFromStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForDelete()
        {
            var uri = DataObject.GetUri();
            var cascadeDeleteOff = OptionsIn.CascadedDelete.False.Value.ToLower();
            var parserCascadedDelete = Parser.CascadedDelete().ToString().ToLower();

            if (!Parser.HasElements() && cascadeDeleteOff.Equals(parserCascadedDelete))
            {
                yield return ValidateObjectExistence(uri);

                // Validate that there are no child data-objects if cascading deletes are not invoked.
                foreach (var dataAdapter in Providers.Cast<IWitsmlDataAdapter>())
                {
                    if (dataAdapter.DataObjectType == typeof (Well) || dataAdapter.DataObjectType == typeof (Wellbore))
                        continue;

                    if (dataAdapter.Any(uri))
                        yield return new ValidationResult(ErrorCodes.NotBottomLevelDataObject.ToString());
                }
            }
            else
            {
                yield return ValidateObjectExistence(uri);
            }
        }

        private ValidationResult ValidateObjectExistence(EtpUri uri)
        {
            // Validate that a Uid was provided
            if (string.IsNullOrWhiteSpace(DataObject.UidWell))
            {
                return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "UidWell" });
            }
            // Validate that a well for the Uid exists
            else if (!_wellDataAdapter.Exists(uri.Parent))
            {
                return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "UidWell" });
            }

            // Validate that a Uid was provided
            if (string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "Uid" });
            }
            // Validate that a wellbore for the Uid exists
            else if (!_wellboreDataAdapter.Exists(uri))
            {
                return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "UidWell", "Uid" });
            }
            return null;
        }
    }
}
