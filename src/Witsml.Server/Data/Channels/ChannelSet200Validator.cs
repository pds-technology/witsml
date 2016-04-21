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

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Provides validation for <see cref="ChannelSet" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{ChannelSet}" />
    [Export(typeof(IDataObjectValidator<ChannelSet>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelSet200Validator : DataObjectValidator<ChannelSet>
    {
        private readonly IWitsmlDataAdapter<ChannelSet> _channelSetDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSet200Validator" /> class.
        /// </summary>
        /// <param name="channelSetDataAdapter">The channel set data adapter.</param>
        [ImportingConstructor]
        public ChannelSet200Validator(IWitsmlDataAdapter<ChannelSet> channelSetDataAdapter)
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
