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
using Energistics.DataAccess.WITSML200;
using System.ComponentModel.Composition;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Provides validation for <see cref="Wellbore" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Energistics.DataAccess.WITSML200.Wellbore}" />
    [Export(typeof(IDataObjectValidator<Wellbore>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Wellbore200Validator : DataObjectValidator<Wellbore>
    {
        private readonly IEtpDataAdapter<Wellbore> _wellboreDataAdapter;
        private readonly IEtpDataAdapter<Well> _wellDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wellbore200Validator"/> class.
        /// </summary>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        [ImportingConstructor]
        public Wellbore200Validator(IEtpDataAdapter<Wellbore> wellboreDataAdapter, IEtpDataAdapter<Well> wellDataAdapter)
        {
            _wellboreDataAdapter = wellboreDataAdapter;
            _wellDataAdapter = wellDataAdapter;
        }

        /// <summary>
        /// Validates the data object while executing PutObject.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForPutObject()
        {
            // Validate parent exists
            //if (!_wellDataAdapter.Exists(DataObject.UidWell))
            //{
            //    yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWell" });
            //}

            // Validate UID does not exist
            //else if (_wellboreDataAdapter.Exists(DataObject.Uid))
            //{
            //    yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            //}

            yield break;
        }
    }
}
