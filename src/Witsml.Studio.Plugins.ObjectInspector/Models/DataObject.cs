//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;
using Energistics.DataAccess.Reflection;
using PDS.Framework;

namespace Witsml.Studio.Plugins.ObjectInspector.Models
{
    /// <summary>
    /// Encapsulates meta-data about an Energistics Data Object
    /// </summary>
    public class DataObject : IEquatable<DataObject>
    {
        /// <summary>
        /// Initializes a new <see cref="DataObject"/> from the specified type.
        /// </summary>
        /// <param name="dataObjectType">The type to initialize from</param>
        /// <exception cref="ArgumentNullException"><paramref name="dataObjectType"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="dataObjectType"/> is not an Energistics Data Object.</exception>
        public DataObject(Type dataObjectType)
        {
            dataObjectType.NotNull(nameof(dataObjectType));
            if (dataObjectType.GetCustomAttribute<EnergisticsDataObjectAttribute>() == null)
                throw new ArgumentException($"{dataObjectType.Name} is not an Energistics Data Object", nameof(dataObjectType));

            DataObjectType = dataObjectType;
            DataProperties = DataProperty.CreateChildProperties(dataObjectType, string.Empty);
        }

        /// <summary>
        /// The type of the Energistics Data Object
        /// </summary>
        public Type DataObjectType { get; }

        /// <summary>
        /// The data properties on this Energistics Data Object
        /// </summary>
        public IReadOnlyCollection<DataProperty> DataProperties { get; }

        /// <summary>
        /// All nested data properties on this Energistics Data Object
        /// </summary>
        public IReadOnlyCollection<DataProperty> NestedDataProperties
        {
            get
            {
                var nestedDataProperties = new List<DataProperty>();
                foreach (var dataProperty in DataProperties)
                {
                    nestedDataProperties.Add(dataProperty);
                    nestedDataProperties.AddRange(dataProperty.DescendantProperties);
                }
                return nestedDataProperties;
            }
        }

        /// <summary>
        /// The standard family the data object is from.
        /// </summary>
        public StandardFamily StandardFamily => DataObjectType.GetCustomAttribute<EnergisticsDataObjectAttribute>().StandardFamily;

        /// <summary>
        /// The data schema version for the object.
        /// </summary>
        public Version DataSchemaVersion => DataObjectType.GetCustomAttribute<EnergisticsDataObjectAttribute>().DataSchemaVersion;

        /// <summary>
        /// The name of the Energistics Data Object
        /// </summary>
        public string Name => DataObjectType.Name;

        /// <summary>
        /// The name of the Energistics Data Object as defined in the data schema.
        /// </summary>
        public string XmlType => DataObjectType.GetCustomAttribute<XmlTypeAttribute>().TypeName;

        /// <summary>
        /// The description of the Energistics Data Object.
        /// </summary>
        public string Description => DataObjectType.GetCustomAttribute<DescriptionAttribute>().Description;

        #region Equality and Inequality        
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(DataObject other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return DataObjectType == other.DataObjectType;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DataObject)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return DataObjectType.GetHashCode();
        }

        /// <summary>
        /// Checks if two <see cref="DataObject"/> instances are equal to each other.
        /// </summary>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <returns>
        /// true if the <paramref name="left" /> object is equal to the <paramref name="right" /> parameter; otherwise, false.
        /// </returns>
        public static bool operator ==(DataObject left, DataObject right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks if two <see cref="DataObject"/> instances are not equal to each other.
        /// </summary>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <returns>
        /// true if the <paramref name="left" /> object is not equal to the <paramref name="right" /> parameter; otherwise, false.
        /// </returns>
        public static bool operator !=(DataObject left, DataObject right)
        {
            return !Equals(left, right);
        }
        #endregion
    }
}
