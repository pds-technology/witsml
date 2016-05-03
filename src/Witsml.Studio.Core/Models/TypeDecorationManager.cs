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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Energistics.DataAccess.Validation;
using PDS.Framework;
using PDS.Witsml.Studio.Core.Converters;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PDS.Witsml.Studio.Core.Models
{
    public static class TypeDecorationManager
    {
        private static readonly ConcurrentBag<Type> _registeredTypes;

        static TypeDecorationManager()
        {
            _registeredTypes = new ConcurrentBag<Type>();
        }

        public static bool IsRegistered(Type type)
        {
            return _registeredTypes.Contains(type);
        }

        public static void AddExpandableObjectConverter(Type type)
        {
            TypeDescriptor.AddAttributes(type, new TypeConverterAttribute(typeof(ExpandableObjectConverter)));
            TypeDescriptor.AddAttributes(type, new ExpandableObjectAttribute());
        }

        public static void AddExpandableListConverter(Type type)
        {
            TypeDescriptor.AddAttributes(type, new TypeConverterAttribute(typeof(ExpandableListConverter)));
            TypeDescriptor.AddAttributes(type, new ExpandableObjectAttribute());
        }

        public static void AddExpandableListConverter<T>(Type type)
        {
            TypeDescriptor.AddAttributes(type, new TypeConverterAttribute(typeof(ExpandableListConverter<T>)));
            TypeDescriptor.AddAttributes(type, new ExpandableObjectAttribute());
        }

        public static void AddExpandableObjectAndListConverter(Type type)
        {
            var listType = typeof(IList<>).MakeGenericType(type);

            AddExpandableObjectConverter(type);
            AddExpandableListConverter(listType);
        }

        public static void Register(Type type)
        {
            if (IsRegistered(type)) return;
            _registeredTypes.Add(type);

            AddExpandableObjectAndListConverter(type);

            type.GetProperties()
                .Where(x => x.GetCustomAttribute<ComponentElementAttribute>() != null ||
                            x.GetCustomAttribute<RecurringElementAttribute>() != null)
                .ForEach(x =>
                {
                    var propertyType = x.GetCustomAttribute<RecurringElementAttribute>() != null
                        ? x.PropertyType.GetGenericArguments().First()
                        : x.PropertyType;

                    Register(propertyType);
                });
        }
    }
}
