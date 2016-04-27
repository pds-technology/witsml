using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDS.Witsml.Server.Data.Transactions
{
    public class MongoTransaction
    {
        public string Tid { get; set; }

        public MongoDbAction Action { get; set; }

        public string Value { get; set; }
    }

    public enum MongoDbAction
    {
        Add,
        Update,
        Delete
    }
}
