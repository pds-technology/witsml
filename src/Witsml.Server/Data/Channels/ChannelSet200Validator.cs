using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML200;
using System.ComponentModel.Composition;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Provides validation for <see cref="ChannelSet" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Energistics.DataAccess.WITSML200.ChannelSet}" />
    [Export(typeof(IDataObjectValidator<ChannelSet>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelSet200Validator : DataObjectValidator<ChannelSet>
    {
        private readonly IEtpDataAdapter<ChannelSet> _channelSetDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSet200Validator" /> class.
        /// </summary>
        /// <param name="channelSetDataAdapter">The channel set data adapter.</param>
        [ImportingConstructor]
        public ChannelSet200Validator(IEtpDataAdapter<ChannelSet> channelSetDataAdapter)
        {
            _channelSetDataAdapter = channelSetDataAdapter;
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            // Validate UID does not exist
            //else if (_channelSetDataAdapter.Exists(DataObject.Uid))
            //{
            //    yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            //}

            yield break;
        }
    }
}
