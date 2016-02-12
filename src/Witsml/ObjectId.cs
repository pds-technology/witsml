namespace PDS.Witsml
{
    public class DataObjectId
    {
        public DataObjectId(string uid, string name)
        {
            Uid = uid;
            Name = name;
        }

        public string Uid { get; private set; }

        public string Name { get; private set; }
    }

    public class WellObjectId : DataObjectId
    {
        public WellObjectId(string uid, string uidWell, string name) : base(uid, name)
        {
            UidWell = uidWell;
        }

        public string UidWell { get; private set; }
    }

    public class WellboreObjectId : WellObjectId
    {
        public WellboreObjectId(string uid, string uidWell, string uidWellbore, string name) : base(uid, uidWell, name)
        {
            UidWellbore = uidWellbore;
        }

        public string UidWellbore { get; private set; }
    }
}
