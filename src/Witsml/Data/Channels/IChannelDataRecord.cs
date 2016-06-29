//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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

using System;
using System.Collections.Generic;
using System.Data;
using PDS.Framework;

namespace PDS.Witsml.Data.Channels
{

    /// <summary>
    /// Index value order
    /// </summary>
    public enum IndexOrder
    {
        /// <summary>
        /// Before
        /// </summary>
        Before,

        /// <summary>
        /// Same index value
        /// </summary>
        Same,

        /// <summary>
        /// After
        /// </summary>
        After
    };

    /// <summary>
    /// Defines the properties and methods that define a channel data record
    /// </summary>
    /// <seealso cref="System.Data.IDataRecord" />
    public interface IChannelDataRecord : IDataRecord
    {
        /// <summary>
        /// Gets or sets the record identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        string Uri { get; set; }

        /// <summary>
        /// Gets the channel data mnemonics.
        /// </summary>
        /// <value>
        /// The channel data mnemonics.
        /// </value>
        string[] Mnemonics { get; }

        /// <summary>
        /// Gets the channel data units.
        /// </summary>
        /// <value>
        /// The channel data units.
        /// </value>
        string[] Units { get; }

        /// <summary>
        /// Gets the channel data null values.
        /// </summary>
        /// <value>
        /// The channel data null values.
        /// </value>
        string[] NullValues { get; }

        /// <summary>
        /// Gets the number of indices for the data.
        /// </summary>
        /// <value>
        /// The number of indices for the data.
        /// </value>
        int Depth { get; }

        /// <summary>
        /// Gets the indices.
        /// </summary>
        /// <value>
        /// The indices.
        /// </value>
        List<ChannelIndexInfo> Indices { get; }

        /// <summary>
        /// Gets the Index for the given channel position
        /// </summary>
        /// <param name="index">The index position.</param>
        /// <returns>The <see cref="ChannelIndexInfo"/> for the given channel index position.</returns>
        ChannelIndexInfo GetIndex(int index = 0);

        /// <summary>
        /// Gets the index value for a given channel position.
        /// </summary>
        /// <param name="index">The index position.</param>
        /// <param name="scale">The scale factor.</param>
        /// <returns>The index value for a given channel position</returns>
        double GetIndexValue(int index = 0, int scale = 0);

        /// <summary>
        /// Gets the index range for a given channel position.
        /// </summary>
        /// <param name="index">The index channel position.</param>
        /// <returns>The index range</returns>
        Range<double?> GetIndexRange(int index = 0);

        /// <summary>
        /// Gets the channel index range for a given channel position
        /// </summary>
        /// <param name="i">The channel position.</param>
        /// <returns></returns>
        Range<double?> GetChannelIndexRange(int i);

        /// <summary>
        /// Gets the date time offset value for a given channel position.
        /// </summary>
        /// <param name="i">The channel position.</param>
        /// <returns>A <see cref="DateTimeOffset"/> value.</returns>
        DateTimeOffset GetDateTimeOffset(int i);

        /// <summary>
        /// Gets the channel value in unix time microseconds for the given channel position.
        /// </summary>
        /// <param name="i">The channel position.</param>
        /// <returns></returns>
        long GetUnixTimeMicroseconds(int i);

        /// <summary>
        /// Determines whether this instance has values.
        /// </summary>
        /// <returns>true if there are any channel values, false otherwise.</returns>
        bool HasValues();

        /// <summary>
        /// Gets the data values formatted as JSON.
        /// </summary>
        /// <returns>A JSON formatted string.</returns>
        string GetJson();

        /// <summary>
        /// Sets the value for the current row for a given channel position.
        /// </summary>
        /// <param name="i">The channel position.</param>
        /// <param name="value">The value.</param>
        void SetValue(int i, object value);

        /// <summary>
        /// Merges the value.
        /// </summary>
        /// <param name="update">The update record.</param>
        /// <param name="rangeSize">The chunk range.</param>
        /// <param name="order">The order of index value</param>
        /// <param name="increasing">if increasting set to <c>true</c> [append].</param>
        void MergeRecord(IChannelDataRecord update, long rangeSize, IndexOrder order, bool increasing);

        /// <summary>
        /// Updates the values.
        /// </summary>
        /// <param name="mnemonicIndexMap">The channel mnemonics index map.</param>
        void UpdateValues(Dictionary<string, int> mnemonicIndexMap);

        /// <summary>
        /// Updates the channel settings.
        /// </summary>
        /// <param name="record">The record.</param>
        /// <param name="withinRange">if set to <c>true</c> [record within range].</param>
        /// <param name="append">if set to <c>true</c> [append].</param>
        void UpdateChannelSettings(IChannelDataRecord record, bool withinRange, bool append);

        /// <summary>
        /// Copies the channel settings.
        /// </summary>
        /// <param name="record">The record.</param>
        void CopyChannelSettings(IChannelDataRecord record);

        /// <summary>
        /// Gets the index of the channel.
        /// </summary>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <returns></returns>
        int GetChannelIndex(string mnemonic);
    }
}
