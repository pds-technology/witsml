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
using System.Linq;
using Energistics.DataAccess.WITSML141.ComponentSchemas;

namespace PDS.WITSMLstudio.Store.Data.MudLogs
{
    /// <summary>
    /// MudLog141Validator
    /// </summary>
    public partial class MudLog141Validator
    {
        /// <summary>
        /// Configures the context.
        /// </summary>
        protected override void ConfigureContext()
        {
            Context.Ignored = new List<string>
            {
                "startMd", "endMd"
            };
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            var geologyIntervals = DataObject.GeologyInterval;

            // Validate common attributes
            foreach (var result in base.ValidateForInsert())
                yield return result;

            if (geologyIntervals != null)
            {
                foreach (var validationResult in ValidateMudlogGeologyIntervals(geologyIntervals))
                    yield return validationResult;
            }
        }

        /// <summary>
        /// Validates the data object while executing UpdateInStore.
        /// </summary>
        /// <returns>
        /// A collection of validation results.
        /// </returns>
        protected override IEnumerable<ValidationResult> ValidateForUpdate()
        {
            // Validate MudLog uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) || string.IsNullOrWhiteSpace(DataObject.UidWellbore) || string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
            }
            else
            {
                var uri = DataObject.GetUri();
                var geologyIntervals = DataObject.GeologyInterval;
                if (geologyIntervals != null)
                {
                    foreach (var validationResult in ValidateMudlogGeologyIntervals(geologyIntervals))
                        yield return validationResult;
                }

                var current = DataAdapter.Get(uri);

                // Validate MudLog does not exist
                if (current == null)
                {
                    yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
                }
            }
        }

        /// <summary>
        /// Validates the data object while executing DeleteFromStore.
        /// </summary>
        /// <returns>
        /// A collection of validation results.
        /// </returns>
        protected override IEnumerable<ValidationResult> ValidateForDelete()
        {
            // Validate MudLog uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) || string.IsNullOrWhiteSpace(DataObject.UidWellbore) || string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
            }
            else
            {
                var uri = DataObject.GetUri();
                var geologyIntervals = DataObject.GeologyInterval;
                if (geologyIntervals != null)
                {
                    foreach (var validationResult in ValidateMudlogGeologyIntervals(geologyIntervals))
                        yield return validationResult;
                }

                var current = DataAdapter.Get(uri);

                // Validate MudLog does not exist
                if (current == null)
                {
                    yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
                }
            }
        }

        /// <summary>
        /// Validates the mudLog geology intervals.
        /// </summary>
        /// <param name="geologyInterval">The mudLog geology intervals.</param>
        /// <returns>
        /// A collection of validation results.
        /// </returns>
        private IEnumerable<ValidationResult> ValidateMudlogGeologyIntervals(List<GeologyInterval> geologyInterval)
        {
            // Only ignore if the UID is present without a value
            if (geologyInterval.Any(s => s.Uid != null && string.IsNullOrWhiteSpace(s.Uid)))
            {
                yield return
                    new ValidationResult(ErrorCodes.MissingElementUidForUpdate.ToString(), new[] { "GeologyInterval", "Uid" });
            }
            else if (geologyInterval.HasDuplicateUids())
            {
                yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "GeologyInterval", "Uid" });
            }
            else if (Context.Function.IsDataNodesValid(ObjectTypes.GetObjectType(DataObject), geologyInterval.Count))
            {
                yield return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] { "GeologyInterval" });
            }
        }
    }
}
