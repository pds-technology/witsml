//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Reflection;

namespace PDS.WITSMLstudio.Data
{
    /// <summary>
    /// Class representing a pseudo-property for reflection purposes for class properties that have both XmlArray and XmlArrayItem defined on them.
    /// This represents a pseudo-property for the XmlArrayitem.
    /// </summary>
    public class NavigatorArrayItemPropertyInfo : PropertyInfo
    {
        /// <summary>
        /// The type of the pseudo-property
        /// </summary>
        public override Type PropertyType { get; }

        /// <summary>
        /// The attributes for this pseudo-property.
        /// </summary>
        /// <remarks>Pseudo-properties do not have any attributes defined.</remarks>
        public override PropertyAttributes Attributes { get; } = new PropertyAttributes();

        /// <summary>
        /// Whether the pseudo-property can be read or not.
        /// </summary>
        /// <remarks>Always returns <c>true</c>.</remarks>
        public override bool CanRead => true;

        /// <summary>
        /// Whether the pseudo-property can be written or not.
        /// </summary>
        /// <remarks>Always returns <c>true</c>.</remarks>
        public override bool CanWrite => true;

        /// <summary>
        /// The name of the pseudo-property.
        /// </summary>
        /// <remarks>Derived from the XmlArrayItem attribute.</remarks>
        public override string Name { get; }

        /// <summary>
        /// The type of the property having both XmlArray and XmlArrayItem defined on it.
        /// </summary>
        public override Type DeclaringType { get; }

        /// <summary>
        /// The type of the property having both XmlArray and XmlArrayItem defined on it.
        /// </summary>
        public override Type ReflectedType { get; }

        /// <summary>
        /// Initializes a new <see cref="NavigatorArrayItemPropertyInfo" /> instance.
        /// </summary>
        /// <param name="declaringType">The type of the property having both XmlArray and XmlArrayItem defined on it.</param>
        /// <param name="propertyType">The type of the property having both XmlArray and XmlArrayItem defined on it.</param>
        /// <param name="name">The name of the pseudo-property.</param>
        public NavigatorArrayItemPropertyInfo(Type declaringType, Type propertyType, string name)
        {
            DeclaringType = declaringType;
            ReflectedType = declaringType;
            PropertyType = propertyType;
            Name = name;
        }

        /// <summary>
        /// Always returns an empty array.
        /// </summary>
        public override object[] GetCustomAttributes(bool inherit)
        {
            return new Attribute[0];
        }

        /// <summary>
        /// Always returns an empty array.
        /// </summary>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return new Attribute[0];
        }

        /// <summary>
        /// Always return false.
        /// </summary>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        /// <summary>
        /// Always returns an empty array.
        /// </summary>
        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return new MethodInfo[0];
        }

        /// <summary>
        /// Always returns an empty array.
        /// </summary>
        public override ParameterInfo[] GetIndexParameters()
        {
            return new ParameterInfo[0];
        }

        /// <summary>
        /// Always returns null.
        /// </summary>
        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            return null;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            return null;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
