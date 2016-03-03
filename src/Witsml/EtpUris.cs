using Energistics.Datatypes;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.Witsml
{
    public static class EtpUris
    {
        public static readonly EtpUri Witsml141 = new EtpUri("eml://witsml1411");
        public static readonly EtpUri Witsml200 = new EtpUri("eml://witsml20");

        public static EtpUri ToUri(this Witsml141.Well entity)
        {
            return Witsml141
                .Append(ObjectTypes.Well, entity.Uid);
        }

        public static EtpUri ToUri(this Witsml141.Wellbore entity)
        {
            return Witsml141
                .Append(ObjectTypes.Well, entity.UidWell)
                .Append(ObjectTypes.Wellbore, entity.Uid);
        }

        public static EtpUri ToUri(this Witsml141.Log entity)
        {
            return Witsml141
                .Append(ObjectTypes.Well, entity.UidWell)
                .Append(ObjectTypes.Wellbore, entity.UidWellbore)
                .Append(ObjectTypes.Log, entity.Uid);
        }

        public static EtpUri ToUri(this Witsml141.ComponentSchemas.LogCurveInfo entity, Witsml141.Log log)
        {
            return log.ToUri()
                .Append(ObjectTypes.LogCurveInfo, entity.Mnemonic.Value);
        }

        public static EtpUri ToUri(this Witsml200.Well entity)
        {
            return Witsml200.Append(ObjectTypes.Well, entity.Uuid);
        }

        public static EtpUri ToUri(this Witsml200.Wellbore entity)
        {
            return Witsml200.Append(ObjectTypes.Wellbore, entity.Uuid);
        }

        public static EtpUri ToUri(this Witsml200.Log entity)
        {
            return Witsml200.Append(ObjectTypes.Log, entity.Uuid);
        }
    }
}
