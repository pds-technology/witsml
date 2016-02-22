using PDS.Framework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MongoDB.Driver;
using Energistics.DataAccess.WITSML141;
using System.ComponentModel.Composition;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Provides validation for adding <see cref="Wellbore"/>
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Energistics.DataAccess.WITSML141.Wellbore}" />
    [Export141(ObjectTypes.Wellbore, typeof(DataObjectValidator<Wellbore>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class AddWellboreValidator : DataObjectValidator<Wellbore>
    {
        private static readonly string ParentDbDocumentName = ObjectNames.Well141;
        private static readonly string DbDocumentName = ObjectNames.Wellbore141;

        IWitsmlDataAdapter<Wellbore> dataAdaptor;
        IWitsmlDataAdapter<Well> parentDataAdaptor;

        [ImportingConstructor]
        public AddWellboreValidator( IWitsmlDataAdapter<Wellbore> dAdaptor, IWitsmlDataAdapter<Well> parentAdaptor)
        {
            dataAdaptor = dAdaptor;
            parentDataAdaptor = parentAdaptor;
        }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            ICollection<ValidationResult> results;

            // Validate the object attributes
            bool success = EntityValidator.TryValidate(DataObject, out results);
            if (!success)
            {
                foreach (ValidationResult r in results)
                    yield return r;
            }
            else
            {
                success = false;
                try
                {
                    // Check if parent well exists.
                    success = parentDataAdaptor.IsEntityExisted(DataObject.UidWell, ParentDbDocumentName);
                }
                catch (MongoQueryException) { }

                if (!success)
                {
                    yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { ErrorCodes.MissingParentDataObject.GetDescription(), "UidWell" });
                    yield break;
                }

                success = false;
                try
                {
                    // Check if the wellbore to be added already exists.
                    success = dataAdaptor.IsEntityExisted(DataObject.Uid, DbDocumentName);
                }
                catch (MongoQueryException) { }

                if (success)
                    yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { ErrorCodes.DataObjectUidAlreadyExists.GetDescription(), "Uid" });
                
            }

            yield break;
        }
    }
}
