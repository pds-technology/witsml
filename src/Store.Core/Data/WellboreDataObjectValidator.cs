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
using Energistics.DataAccess;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides common validation for wellbore child data objects.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <typeparam name="TWellbore">The type of the wellbore.</typeparam>
    /// <typeparam name="TWell">The type of the well.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.DataObjectValidator{TObject}" />
    public abstract class WellboreDataObjectValidator<TObject, TWellbore, TWell> : DataObjectValidator<TObject>
        where TObject : IWellboreObject
        where TWellbore : IWellObject
        where TWell : IDataObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WellboreDataObjectValidator{TObject, TWellbore, TWell}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        protected WellboreDataObjectValidator(IContainer container, IWitsmlDataAdapter<TObject> dataAdapter, IWitsmlDataAdapter<TWellbore> wellboreDataAdapter, IWitsmlDataAdapter<TWell> wellDataAdapter) : base(container)
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
            var wellbore = WellboreDataAdapter.Get(uriWellbore, "Uid", "UidWell");

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
            else if (Context.Function != Functions.PutObject && DataAdapter.Exists(uri))
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
            return ValidateObjectExistence();
        }

        /// <summary>
        /// Validates the data object while executing DeleteFromStore.
        /// </summary>
        /// <returns>
        /// A collection of validation results.
        /// </returns>
        protected override IEnumerable<ValidationResult> ValidateForDelete()
        {
            return ValidateObjectExistence();
        }

        /// <summary>
        /// Validates the data object existence.
        /// </summary>
        /// <returns>
        /// A collection of validation results.
        /// </returns>
        protected virtual IEnumerable<ValidationResult> ValidateObjectExistence()
        {
            var uri = DataObject.GetUri();

            // Validate uid properties
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) ||
                string.IsNullOrWhiteSpace(DataObject.UidWellbore) ||
                string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return
                    new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] {"Uid", "UidWell", "UidWellbore"});
            }
            // Validate UID exists
            else if (!DataAdapter.Exists(uri))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] {"UidWell", "Uid"});
            }
        }
    }
}
