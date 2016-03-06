using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Energistics.DataAccess;
using log4net;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Server.Data
{
    public class WitsmlQueryTemplate<T>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlQueryTemplate<T>));
        private static readonly IList<Type> ExcludeTypes = new List<Type>();

        static WitsmlQueryTemplate()
        {
            Exclude<Witsml131.ComponentSchemas.CustomData>();
            Exclude<Witsml131.ComponentSchemas.DocumentInfo>();

            Exclude<Witsml141.ComponentSchemas.CustomData>();
            Exclude<Witsml141.ComponentSchemas.DocumentInfo>();
            Exclude<Witsml141.ComponentSchemas.ExtensionAny>();
            Exclude<Witsml141.ComponentSchemas.ExtensionNameValue>();
        }

        private static void Exclude<V>()
        {
            ExcludeTypes.Add(typeof(V));
        }

        public T AsObject()
        {
            return (T)CreateTemplate(typeof(T));
        }

        public List<T> AsList()
        {
            return new List<T>() { AsObject() };
        }

        public string AsXml()
        {
            return EnergisticsConverter.ObjectToXml(AsObject());
        }

        public string AsXml<TList>() where TList : IEnergisticsCollection
        {
            var list = CreateTemplate(typeof(TList));
            return EnergisticsConverter.ObjectToXml(list);
        }

        protected object CreateTemplate(Type objectType)
        {
            if (objectType == null || ExcludeTypes.Contains(objectType))
            {
                return null;
            }
            if (objectType == typeof(string))
            {
                return "abc";
            }
            if (objectType == typeof(bool))
            {
                return false;
            }
            if (objectType == typeof(short) || objectType == typeof(int) || objectType == typeof(long) || 
                objectType == typeof(double) || objectType == typeof(float) || objectType == typeof(decimal))
            {
                return Convert.ChangeType(1, objectType);
            }
            if (typeof(DateTime).IsAssignableFrom(objectType))
            {
                return DateTime.MinValue;
            }
            if (objectType.IsEnum)
            {
                return Enum.GetValues(objectType).GetValue(0);
            }
            if (objectType.IsGenericType)
            {
                var genericType = objectType.GetGenericTypeDefinition();

                if (genericType == typeof(Nullable<>))
                {
                    return Activator.CreateInstance(objectType, CreateTemplate(Nullable.GetUnderlyingType(objectType)));
                }
                if (genericType == typeof(List<>))
                {
                    var childType = objectType.GetGenericArguments()[0];
                    var list = Activator.CreateInstance(objectType) as IList;
                    list.Add(CreateTemplate(childType));
                    return list;
                }
            }
            if (objectType.IsAbstract)
            {
                var concreteType = objectType.Assembly.GetTypes()
                    .Where(x => !x.IsAbstract && objectType.IsAssignableFrom(x))
                    .FirstOrDefault();

                return CreateTemplate(concreteType);
            }

            var dataObject = Activator.CreateInstance(objectType);

            foreach (var property in objectType.GetProperties())
            {
                try
                {
                    if (property.CanWrite && !IsIgnored(property))
                    {
                        property.SetValue(dataObject, CreateTemplate(property.PropertyType));
                    }
                }
                catch
                {
                    Console.WriteLine("Error setting property value. Type: {0}; Property: {1}", objectType.FullName, property.Name);
                }
            }

            return dataObject;
        }

        private bool IsIgnored(PropertyInfo property)
        {
            return property.GetCustomAttributes<XmlIgnoreAttribute>().Any();
        }
    }
}
