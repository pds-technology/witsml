using PDS.Framework;

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

        public bool Equals(DataObjectId id)
        {
            if (id == null || GetType() != id.GetType())
                return false;

            if (ReferenceEquals(id, this))
                return true;

            return ToString().EqualsIgnoreCase(id.ToString());
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            if (ReferenceEquals(obj, this))
                return true;

            return ToString().EqualsIgnoreCase(obj.ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}", Uid);
        }
    }

    public class WellObjectId : DataObjectId
    {
        public WellObjectId(string uid, string uidWell, string name) : base(uid, name)
        {
            UidWell = uidWell;
        }

        public string UidWell { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}", UidWell, Uid);
        }
    }

    public class WellboreObjectId : WellObjectId
    {
        public WellboreObjectId(string uid, string uidWell, string uidWellbore, string name) : base(uid, uidWell, name)
        {
            UidWellbore = uidWellbore;
        }

        public string UidWellbore { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}", UidWell, UidWellbore, Uid);
        }
    }
}
