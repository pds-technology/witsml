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
    public class ChannelDataValuesAdapter : MongoDbDataAdapter<ChannelDataValues>
    {
        [ImportingConstructor]
        public ChannelDataValuesAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectTypes.ChannelDataValues, ObjectTypes.Uid)
        {

        }
    }
}
