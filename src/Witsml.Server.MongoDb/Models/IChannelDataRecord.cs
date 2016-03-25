using System;
using System.Collections.Generic;
using System.Data;
using PDS.Framework;

namespace PDS.Witsml.Server.Models
{
    public interface IChannelDataRecord : IDataRecord
    {
        string Id { get; set; }

        string Uid { get; set; }

        string[] Mnemonics { get; }

        string[] Units { get; }

        List<ChannelIndexInfo> Indices { get; }

        Range<double> GetIndexRange(int index = 0);

        double GetIndexValue(int index = 0);

        long GetUnixTimeSeconds(int i);

        DateTimeOffset GetDateTimeOffset(int i);

        string GetJson();
    }
}
