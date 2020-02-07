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
    /// Provides common validation for well child data objects other than wellbore.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <typeparam name="TWell">The type of the well.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.DataObjectValidator{TObject}" />
    public abstract class WellDataObjectValidator<TObject, TWell> : DataObjectValidator<TObject>
        where TObject : IWellObject
        where TWell : IDataObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WellDataObjectValidator{TObject, TWell}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        protected WellDataObjectValidator(IContainer container, IWitsmlDataAdapter<TObject> dataAdapter, IWitsmlDataAdapter<TWell> wellDataAdapter) : base(container)
        {
            DataAdapter = dataAdapter;
            WellDataAdapter = wellDataAdapter;
        }

        /// <summary>
        /// Gets the data adapter.
        /// </summary>
        protected IWitsmlDataAdapter<TObject> DataAdapter { get; }

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
            // Validate parent uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell))
            {
                yield return new ValidationResult(ErrorCodes.MissingElementUidForAdd.ToString(), new[] { "UidWell" });
            }
            else
            {
                var uri = DataObject.GetUri();
                var uriWell = uri.Parent;
                var well = WellDataAdapter.Get(uriWell);

                // Validate parent exists
                if (well == null)
                {
                    yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWell" });
                }

                // Validate parent uid case
                else if (!well.Uid.Equals(DataObject.UidWell))
                {
                    yield return new ValidationResult(ErrorCodes.IncorrectCaseParentUid.ToString(), new[] { "UidWell" });
                }

                // Validate UID does not exist
                else if (Context.Function != Functions.PutObject && DataAdapter.Exists(uri))
                {
                    yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
                }
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
        /// Validates the existence of the well object and its parent.
        /// </summary>
        /// <returns>
        /// A collection of validation results.
        /// </returns>
        protected IEnumerable<ValidationResult> ValidateObjectExistence()
        {
            var uri = DataObject.GetUri();

            // Validate uid properties
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) ||
                string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return
                    new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "Uid", "UidWell" });
            }
            // Validate that a well for the Uid exists
            else if (!WellDataAdapter.Exists(uri.Parent))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "UidWell" });
            }
            // Validate UID exists
            else if (!DataAdapter.Exists(uri))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "UidWell", "Uid" });
            }
        }
    }
}
