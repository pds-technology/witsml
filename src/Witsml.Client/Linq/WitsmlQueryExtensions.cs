using System.Linq;
using Energistics.DataAccess;

namespace PDS.Witsml.Client.Linq
{
    public static class WitsmlQueryExtensions
    {
        public static T GetByUid<T>(this IWitsmlQuery<T> query, string uid) where T : IDataObject
        {
            return query.Where(x => x.Uid == uid).FirstOrDefault();
        }

        public static T GetByUid<T>(this IWitsmlQuery<T> query, string uidWell, string uid) where T : IWellObject
        {
            return query.Where(x => x.UidWell == uidWell && x.Uid == uid).FirstOrDefault();
        }

        public static T GetByUid<T>(this IWitsmlQuery<T> query, string uidWell, string uidWellbore, string uid) where T : IWellboreObject
        {
            return query.Where(x => x.UidWell == uidWell && x.UidWellbore == uidWellbore && x.Uid == uid).FirstOrDefault();
        }

        public static T GetByName<T>(this IWitsmlQuery<T> query, string name) where T : IDataObject
        {
            return query.Where(x => x.Name == name).FirstOrDefault();
        }

        public static T GetByName<T>(this IWitsmlQuery<T> query, string nameWell, string name) where T : IWellObject
        {
            return query.Where(x => x.NameWell == nameWell && x.Name == name).FirstOrDefault();
        }

        public static T GetByName<T>(this IWitsmlQuery<T> query, string nameWell, string nameWellbore, string name) where T : IWellboreObject
        {
            return query.Where(x => x.NameWell == nameWell && x.NameWellbore == nameWellbore && x.Name == name).FirstOrDefault();
        }
    }
}
