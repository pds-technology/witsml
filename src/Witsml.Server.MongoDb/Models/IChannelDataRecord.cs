using System.Collections.Generic;
using System.Data;

namespace PDS.Witsml.Server.Models
{
    public interface IChannelDataRecord : IDataRecord
    {
        string Uid { get; set; }

        string[] Mnemonics { get; }

        string[] Units { get; }

        List<ChannelIndexInfo> Indices { get; }

        long GetUnixTimeSeconds(int i);

        string GetJson();
    }
}
