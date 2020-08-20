using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Energistics.DataAccess;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Data.Trajectories
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TrajectoryDataReader : IDataReader
    {
        /// <summary>
        /// 
        /// </summary>
        public const string TrajectoryTypeName = "Trajectory";
        /// <summary>
        /// 
        /// </summary>
        public const string TrajectoryStationTypeName = "TrajectoryStation";
        /// <summary>
        /// 
        /// </summary>
        public const string LocationTypeName = "Location";

        private int _current = -1;
        private Adapters.Trajectory _trajectory;

        private readonly Dictionary<string, Dictionary<string, PropertyInfo>> _propertyMappings = new Dictionary<string, Dictionary<string, PropertyInfo>>();
        private readonly List<string> _propertyPaths = new List<string>();
        private readonly List<Type> _propertyTypes = new List<Type>();
        private readonly Dictionary<string, int> _propertyPathsOrdinalMap;

        /// <summary>
        /// Creates a reader from a Trajectory adapter
        /// </summary>
        /// <param name="data"></param>
        public TrajectoryDataReader(Adapters.Trajectory data)
        {
            _trajectory = data;
            var wrappedType = data.WrappedTrajectory.GetType();

            _propertyPaths.Add(TrajectoryTypeName);
            _propertyTypes.Add(wrappedType);

            AddMappedProperties(wrappedType, TrajectoryTypeName);

            _propertyPathsOrdinalMap = _propertyPaths.Select((path, index) => new {path, index})
                .ToDictionary(entry => entry.path, entry => entry.index);
        }

        /// <summary>
        /// Creates a reader from an xml string
        /// </summary>
        /// <param name="data"></param>
        public TrajectoryDataReader(string data) : this(WitsmlParser.Parse(data))
        {
        }

        /// <summary>
        /// Creates a reader from a data object
        /// </summary>
        /// <param name="data"></param>
        public TrajectoryDataReader(object data) : this(new Adapters.Trajectory(data))
        {
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            _current = -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public object this[int i] => this[GetName(i)];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object this[string name] => GetPropertyValue(name);

        /// <summary>
        /// 
        /// </summary>
        public int Depth => 0;

        /// <summary>
        /// 
        /// </summary>
        public bool IsClosed => null == _trajectory?.TrajectoryStation || _current >= _trajectory.TrajectoryStation.Count;

        /// <summary>
        /// 
        /// </summary>
        public int RecordsAffected => _trajectory?.TrajectoryStation?.Count ?? 0;

        /// <summary>
        /// 
        /// </summary>
        public int FieldCount => _propertyPaths.Count;

        /// <summary>
        /// 
        /// </summary>
        public void Close()
        {
            _trajectory = null;
            _current = -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public bool GetBoolean(int i)
        {
            return (bool)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public byte GetByte(int i)
        {
            return (byte)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <param name="fieldOffset"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferoffset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public char GetChar(int i)
        {
            return (char)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <param name="fieldoffset"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferoffset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public string GetDataTypeName(int i)
        {
            return GetFieldType(i).Name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public DateTime GetDateTime(int i)
        {
            return (DateTime)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public decimal GetDecimal(int i)
        {
            return (decimal)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public double GetDouble(int i)
        {
            return (double)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Type GetFieldType(int i)
        {
            return _propertyTypes[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public float GetFloat(int i)
        {
            return (float)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Guid GetGuid(int i)
        {
            return (Guid)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public short GetInt16(int i)
        {
            return (short)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public int GetInt32(int i)
        {
            return (int)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public long GetInt64(int i)
        {
            return (long)this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public string GetName(int i)
        {
            return _propertyPaths[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetOrdinal(string name)
        {
            int index;
            if (_propertyPathsOrdinalMap.TryGetValue(name, out index))
                return index;

            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public string GetString(int i)
        {
            return this[i].ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public object GetValue(int i)
        {
            return this[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public int GetValues(object[] values)
        {
            int i = 0;
            for (; i < FieldCount && i < values.Length; i++)
            {
                values[i] = this[i];
            }

            return i;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public bool IsDBNull(int i)
        {
            return this[i] == DBNull.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool NextResult()
        {
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Read()
        {
            if (_current < RecordsAffected)
                ++_current;

            return !IsClosed;
        }

        private void AddMappedProperties(Type type, string path)
        {
            if (type.IsPrimitive || type.IsEnum || null == type.GetCustomAttribute<XmlTypeAttribute>())
                return;

            var typeName = type.Name;

            var properties = type.GetProperties().Where(propertyInfo =>
            {
                var elementInfo = XmlAttributeCache<XmlElementAttribute>.GetCustomAttribute(propertyInfo);
                var attributeInfo = XmlAttributeCache<XmlAttributeAttribute>.GetCustomAttribute(propertyInfo);
                var textInfo = XmlAttributeCache<XmlTextAttribute>.GetCustomAttribute(propertyInfo);

                return null != elementInfo || null != attributeInfo || null != textInfo;
            }).ToList();

            var propertiesMap = properties.ToDictionary(property => property.Name);

            _propertyPaths.AddRange(properties.Select(propertyInfo => $"{path}.{propertyInfo.Name}"));
            _propertyTypes.AddRange(properties.Select(propertyInfo =>
            {
                var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

                // Handle recurring element properties
                if (propertyType.IsGenericType && typeof(IList).IsAssignableFrom(propertyType))
                {
                    propertyType = propertyType.GetGenericArguments().First();
                }

                return propertyType;
            }));

            _propertyMappings[typeName] = propertiesMap;

            foreach (var propertyInfo in propertiesMap.Values)
            {
                var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

                // Handle recurring element properties
                if (propertyType.IsGenericType && typeof(IList).IsAssignableFrom(propertyType))
                {
                    propertyType = propertyType.GetGenericArguments().First();
                }

                if (type.IsPrimitive || type.IsEnum || null == propertyType.GetCustomAttribute<XmlTypeAttribute>())
                    continue;

                AddMappedProperties(propertyType, $"{path}.{propertyInfo.Name}");
            }
        }

        private object GetPropertyValue(string propertyPath)
        {
            if (IsClosed)
                return null;
            //if (string.IsNullOrWhiteSpace(propertyPath))
            //    return null;

            var properties = propertyPath.Split('.');

            if (!properties.Any())
                return null;

            var parentProperty = properties[0];

            // first property should specify the object type
            if (!TrajectoryTypeName.Equals(parentProperty))
                return null;

            object instance = _trajectory.WrappedTrajectory;
            var propertyMap = _propertyMappings[TrajectoryTypeName];

            foreach (var property in properties.Skip(1))
            {
                // check whether the property is mapped
                if (null == propertyMap || !propertyMap.ContainsKey(property))
                    return null;

                var propertyInfo = propertyMap[property];

                var list = instance as IList;

                if (null != list)
                {
                    if (LocationTypeName.Equals(parentProperty))
                    {
                        // attempt to find the first instance with a valid property value
                        instance = list.Cast<object>().Select(item => propertyInfo.GetValue(item)).FirstOrDefault(item => null != item);
                    }
                    else
                    {
                        if (TrajectoryStationTypeName.Equals(parentProperty))
                        {
                            // retrieve current trajectory station
                            instance = list[_current];
                        }
                        else
                        {
                            // TODO: Handle indexing of other list based properties
                            instance = list.Count > 0 ? list[0] : null;
                        }

                        if (null == instance)
                            return null;

                        instance = propertyInfo.GetValue(instance);
                    }
                }
                else
                {
                    instance = propertyInfo.GetValue(instance);
                }

                if (null == instance)
                    return null;

                var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

                // Handle recurring element properties
                if (propertyType.IsGenericType && typeof(IList).IsAssignableFrom(propertyType))
                {
                    propertyType = propertyType.GetGenericArguments().First();
                }

                propertyMap = _propertyMappings.ContainsKey(propertyType.Name) ? _propertyMappings[propertyType.Name] : null;

                parentProperty = property;
            }

            return instance is Timestamp ? (DateTimeOffset)(Timestamp)instance : instance;
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    Close();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.

                _disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TrajectoryDataReader() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
