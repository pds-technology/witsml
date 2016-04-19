//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
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
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;

namespace Energistics.Protocol.ChannelDataFrame
{
    /// <summary>
    /// Defines the interface that must be implemented by the producer role of the ChannelDataFrame protocol.
    /// </summary>
    /// <seealso cref="Energistics.Common.IProtocolHandler" />
    [ProtocolRole(Protocols.ChannelDataFrame, "producer", "consumer")]
    public interface IChannelDataFrameProducer : IProtocolHandler
    {
        /// <summary>
        /// Sends a ChannelMetadata message to a consumer.
        /// </summary>
        /// <param name="channelMetadata">The channel metadata.</param>
        void ChannelMetadata(ChannelMetadata channelMetadata);

        /// <summary>
        /// Sends a ChannelDataFrameSet message to a customer.
        /// </summary>
        /// <param name="channelIds">The channel ids.</param>
        /// <param name="dataFrames">The data frames.</param>
        void ChannelDataFrameSet(IList<long> channelIds, IList<DataFrame> dataFrames);

        /// <summary>
        /// Handles the RequestChannelData event from a customer.
        /// </summary>
        event ProtocolEventHandler<RequestChannelData, ChannelMetadata> OnRequestChannelData;
    }
}
