using System.Reflection;
using Avro;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Energistics.Common
{
    public class EtpContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType == typeof(Schema))
            {
                property.ShouldSerialize = (instance) => false;
            }

            return property;
        }
    }
}
