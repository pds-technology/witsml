using System;
using MongoDB.Bson.Serialization;

namespace PDS.Witsml.Server.Data
{
    public class UidGenerator : IIdGenerator
    {
        private static readonly string EmptyUid = Guid.Empty.ToString();
        private static readonly Lazy<UidGenerator> _generator;

        static UidGenerator()
        {
            _generator = new Lazy<UidGenerator>();
        }

        public static UidGenerator Instance
        {
            get { return _generator.Value; }
        }

        public object GenerateId(object container, object document)
        {
            return Guid.NewGuid().ToString();
        }

        public bool IsEmpty(object id)
        {
            var uid = String.Format("{0}", id);
            return String.IsNullOrWhiteSpace(uid) || EmptyUid.Equals(uid);
        }
    }
}
