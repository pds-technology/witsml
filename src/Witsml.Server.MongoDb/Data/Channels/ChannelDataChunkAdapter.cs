using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Channels
{
    [Export]
    public class ChannelDataChunkAdapter : MongoDbDataAdapter<ChannelDataValues>
    {
        [ImportingConstructor]
        public ChannelDataChunkAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectTypes.ChannelDataValues, ObjectTypes.Uid)
        {

        }

        public void SaveChannelDataValues(ChannelDataReader reader)
        {
            
        }
    }
}
