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
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Provides validation for <see cref="Wellbore" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Wellbore}" />
    [Export(typeof(IDataObjectValidator<Wellbore>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Wellbore131Validator : DataObjectValidator<Wellbore>
    {
        private readonly IWitsmlDataAdapter<Wellbore> _wellboreDataAdapter;
        private readonly IWitsmlDataAdapter<Well> _wellDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wellbore131Validator"/> class.
        /// </summary>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        [ImportingConstructor]
        public Wellbore131Validator(IWitsmlDataAdapter<Wellbore> wellboreDataAdapter, IWitsmlDataAdapter<Well> wellDataAdapter)
        {
            _wellboreDataAdapter = wellboreDataAdapter;
            _wellDataAdapter = wellDataAdapter;
        }

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
            else if (_wellboreDataAdapter.Exists(uri))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            }
        }
    }
}
