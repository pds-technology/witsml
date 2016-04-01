using System;
using System.Collections.Generic;
using System.Data;
using PDS.Framework;

namespace PDS.Witsml.Data.Channels
{
    public interface IChannelDataRecord : IDataRecord
    {
        string Id { get; set; }

        string Uri { get; set; }

        string[] Mnemonics { get; }

        string[] Units { get; }

        int Depth { get; }

        List<ChannelIndexInfo> Indices { get; }

        ChannelIndexInfo GetIndex(int index = 0);

        double GetIndexValue(int index = 0, int scale = 0);

        Range<double?> GetIndexRange(int index = 0);

        Range<double?> GetChannelIndexRange(int i);

        DateTimeOffset GetDateTimeOffset(int i);

        long GetUnixTimeSeconds(int i);

        bool HasValues();

        string GetJson();

        void SetValue(int i, object value);
    }
}
