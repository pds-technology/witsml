using Energistics.DataAccess;
using Energistics.Datatypes;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using AbstractObject = Energistics.DataAccess.WITSML200.ComponentSchemas.AbstractObject;

namespace PDS.Witsml
{
    public static class EtpUris
    {
        public static readonly EtpUri Witsml131 = new EtpUri("eml://witsml1311");
        public static readonly EtpUri Witsml141 = new EtpUri("eml://witsml1411");
        public static readonly EtpUri Witsml200 = new EtpUri("eml://witsml20");

        public static EtpUri GetUriFamily(this IDataObject entity)
        {
            if (entity.GetType().Namespace.Contains("WITSML131"))
                return Witsml131;

            return Witsml141;
        }

        public static EtpUri GetUri(this IDataObject entity)
        {
            return entity.GetUriFamily()
                .Append(ObjectTypes.GetObjectType(entity), entity.Uid);
        }

        public static EtpUri GetUri(this IWellObject entity)
        {
            return entity.GetUriFamily()
                .Append(ObjectTypes.Well, entity.UidWell)
                .Append(ObjectTypes.GetObjectType(entity), entity.Uid);
        }

        public static EtpUri GetUri(this IWellboreObject entity)
        {
            return entity.GetUriFamily()
                .Append(ObjectTypes.Well, entity.UidWell)
                .Append(ObjectTypes.Wellbore, entity.UidWellbore)
                .Append(ObjectTypes.GetObjectType(entity), entity.Uid);
        }

        public static EtpUri GetUri(this AbstractObject entity)
        {
            return Witsml200
                .Append(ObjectTypes.GetObjectType(entity), entity.Uuid);
        }

        public static EtpUri GetUri(this Witsml141.ComponentSchemas.LogCurveInfo entity, Witsml141.Log log)
        {
            return log.GetUri()
                .Append(ObjectTypes.LogCurveInfo, entity.Mnemonic.Value);
        }

        public static EtpUri GetUri(this Witsml200.ChannelSet entity, Witsml200.Log log)
        {
            return log.GetUri()
                .Append(ObjectTypes.ChannelSet, entity.Uuid);
        }

        public static EtpUri GetUri(this Witsml200.Channel entity, Witsml200.Log log, Witsml200.ChannelSet channelSet)
        {
            return channelSet.GetUri(log)
                .Append(ObjectTypes.Channel, entity.Mnemonic);
        }
    }
}
