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

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Provides validation for <see cref="Well" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Well}" />
    [Export(typeof(IDataObjectValidator<Well>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Well141Validator : DataObjectValidator<Well>
    {
        private readonly IWitsmlDataAdapter<Well> _wellDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Well141Validator"/> class.
        /// </summary>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        [ImportingConstructor]
        public Well141Validator(IWitsmlDataAdapter<Well> wellDataAdapter)
        {
            _wellDataAdapter = wellDataAdapter;
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            var uri = DataObject.GetUri();

            // Validate UID does not exist
            if (_wellDataAdapter.Exists(uri))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] {"Uid"});
            }
        }

        /// <summary>
        /// Validates the data object while executing UpdateInStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForUpdate()
        {
            var uri = DataObject.GetUri();

            // Validate that a Uid was provided
            if (string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] {"Uid"});
            }
            // Validate that a well for the Uid exists
            else if (!_wellDataAdapter.Exists(uri))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] {"Uid"});
            }
        }
    }
}
