//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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
using System.Data;
using System.Linq;
using System.Reflection;
using PDS.Framework;
using PDS.Witsml.Server.Converters;
using PDS.Witsml.Server.Models;
using PetaPoco;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Provides mappings for WITSML data object types.
    /// </summary>
    /// <seealso cref="PetaPoco.ConventionMapper" />
    public abstract class DataObjectMapper : ConventionMapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectMapper" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataObjectTypes">The data object types.</param>
        protected DataObjectMapper(IContainer container, params Type[] dataObjectTypes)
        {
            Container = container;
            DataObjectTypes = dataObjectTypes.ToList();
            ToDbConverter = ResolveToDbConverter;
            FromDbConverter = ResolveFromDbConverter;
        }

        /// <summary>
        /// Gets the composition container that can be used for dependency injection.
        /// </summary>
        /// <value>The composition container.</value>
        protected IContainer Container { get; }

        /// <summary>
        /// Gets the type of the data object.
        /// </summary>
        /// <value>The type of the data object.</value>
        public List<Type> DataObjectTypes { get; }

        /// <summary>
        /// Gets or sets the data object mapping.
        /// </summary>
        /// <value>The data object mapping.</value>
        public ObjectMapping Mapping { get; set; }

        /// <summary>
        /// Resolves the <see cref="IDbValueConverter"/> for the specified property.
        /// </summary>
        /// <param name="sourceProperty">The source property.</param>
        /// <returns>The method to use for converting to a provider specific data type.</returns>
        protected virtual Func<object, object> ResolveToDbConverter(PropertyInfo sourceProperty)
        {
            var converter = Resolve(sourceProperty.PropertyType);
            if (converter == null) return null;

            return converter.ConvertToDb;
        }

        /// <summary>
        /// Resolves the <see cref="IDbValueConverter" /> for the specified property.
        /// </summary>
        /// <param name="targetProperty">The target property.</param>
        /// <param name="sourceType">Type of the source.</param>
        /// <returns>The method to use for converting from a provider specific data type.</returns>
        protected virtual Func<IDataReader, object, object> ResolveFromDbConverter(PropertyInfo targetProperty, Type sourceType)
        {
            var mapping = Mapping.GetColumn(targetProperty.Name);
            IDbValueConverter converter = null;

            // Resolve converter from column configuration
            if (!string.IsNullOrWhiteSpace(mapping?.Converter))
                converter = Resolve<IDbValueConverter>(mapping.Converter);

            // Resolve converter based on property type
            if (converter == null)
                converter = Resolve(targetProperty.PropertyType);

            // Return null if no converter resolved
            if (converter == null)
                return null;

            return (reader, value) => converter.ConvertFromDb(Mapping, reader, targetProperty.Name, value);
        }

        /// <summary>
        /// Resolves the <see cref="IDbValueConverter"/> for specified data type.
        /// </summary>
        /// <param name="type">The data type.</param>
        /// <returns>
        /// The <see cref="IDbValueConverter"/> instance, or null if the converter is not configured for the data type.
        /// </returns>
        protected IDbValueConverter Resolve(Type type)
        {
            var valueType = Nullable.GetUnderlyingType(type);
            type = valueType ?? type;

            // NOTE: not attempting to find a converter for System types
            if (type.Namespace == typeof(string).Namespace)
                return null;

            return Resolve<IDbValueConverter>(type.FullName);
        }

        /// <summary>
        /// Resolves a resource for specified data type.
        /// </summary>
        /// <param name="contractName">The contract name.</param>
        /// <returns>
        /// An object instance, or null if the resource is not configured for the data type.
        /// </returns>
        protected T Resolve<T>(string contractName)
        {
            try
            {
                return Container.Resolve<T>(contractName);
            }
            catch (ContainerException)
            {
                return default(T);
            }
        }
    }
}
