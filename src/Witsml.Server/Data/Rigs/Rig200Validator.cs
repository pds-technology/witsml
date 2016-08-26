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
using Energistics.DataAccess.WITSML200;
using PDS.Framework;

namespace PDS.Witsml.Server.Data.Rigs
{
    /// <summary>
    /// Provides validation for <see cref="Rig" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Rig}" />
    [Export(typeof(IDataObjectValidator<Rig>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Rig200Validator : DataObjectValidator<Rig>
    {
        private readonly IWitsmlDataAdapter<Rig> _rigDataAdapter;
        private readonly IWitsmlDataAdapter<Wellbore> _wellboreDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rig200Validator" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="rigDataAdapter">The rig data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        [ImportingConstructor]
        public Rig200Validator(IContainer container, IWitsmlDataAdapter<Rig> rigDataAdapter, IWitsmlDataAdapter<Wellbore> wellboreDataAdapter) : base(container)
        {
            _rigDataAdapter = rigDataAdapter;
            _wellboreDataAdapter = wellboreDataAdapter;
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            // Validate parent uid property
            //if (string.IsNullOrWhiteSpace(DataObject.UidWellbore))
            //{
            //    yield return new ValidationResult(ErrorCodes.MissingParentUid.ToString(), new[] { "UidWellbore" });
            //}

            // Validate parent exists
            //else if (!_wellboreDataAdapter.Exists(DataObject.UidWellbore))
            //{
            //    yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWellbore" });
            //}

            // Validate UID does not exist
            //else if (_rigDataAdapter.Exists(DataObject.Uid))
            //{
            //    yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            //}

            yield break;
        }
    }
}
