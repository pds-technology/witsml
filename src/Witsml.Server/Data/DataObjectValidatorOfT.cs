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
using Energistics.DataAccess;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Provides common validation for child data objects.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <typeparam name="TWellbore">The type of the wellbore.</typeparam>
    /// <typeparam name="TWell">The type of the well.</typeparam>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{TObject}" />
    public abstract class DataObjectValidator<TObject, TWellbore, TWell> : DataObjectValidator<TObject>
        where TObject : IWellboreObject
        where TWellbore : IWellObject
        where TWell : IDataObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectValidator{TObject, TWellbore, TWell}"/> class.
        /// </summary>
        /// <param name="dataAdapter">The data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        protected DataObjectValidator(IWitsmlDataAdapter<TObject> dataAdapter, IWitsmlDataAdapter<TWellbore> wellboreDataAdapter, IWitsmlDataAdapter<TWell> wellDataAdapter)
        {
            DataAdapter = dataAdapter;
            WellboreDataAdapter = wellboreDataAdapter;
            WellDataAdapter = wellDataAdapter;
        }

        /// <summary>
        /// Gets the data adapter.
        /// </summary>
        protected IWitsmlDataAdapter<TObject> DataAdapter { get; }

        /// <summary>
        /// Gets the wellbore data adapter.
        /// </summary>
        protected IWitsmlDataAdapter<TWellbore> WellboreDataAdapter { get; }

        /// <summary>
        /// Gets the well data adapter.
        /// </summary>
        protected IWitsmlDataAdapter<TWell> WellDataAdapter { get; }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            var uri = DataObject.GetUri();
            var uriWellbore = uri.Parent;
            var uriWell = uriWellbore.Parent;
            var wellbore = WellboreDataAdapter.Get(uriWellbore);

            // Validate parent uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell))
            {
                yield return new ValidationResult(ErrorCodes.MissingElementUidForAdd.ToString(), new[] { "UidWell" });
            }
            // Validate parent uid property
            else if (string.IsNullOrWhiteSpace(DataObject.UidWellbore))
            {
                yield return new ValidationResult(ErrorCodes.MissingElementUidForAdd.ToString(), new[] { "UidWellbore" });
            }

            // Validate parent exists
            else if (!WellDataAdapter.Exists(uriWell))
            {
                yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWell" });
            }
            // Validate parent exists
            else if (wellbore == null)
            {
                yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWellbore" });
            }

            // Validate parent uid case
            else if (!wellbore.UidWell.Equals(DataObject.UidWell) || !wellbore.Uid.Equals(DataObject.UidWellbore))
            {
                yield return new ValidationResult(ErrorCodes.IncorrectCaseParentUid.ToString(), new[] { "UidWell", "UidWellbore" });
            }

            // Validate UID does not exist
            else if (DataAdapter.Exists(uri))
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

            // Validate uid properties
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) ||
                string.IsNullOrWhiteSpace(DataObject.UidWellbore) ||
                string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
            }
            // Validate UID exists
            else if (!DataAdapter.Exists(uri))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "UidWell", "Uid" });
            }
        }
    }
}
