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
using System.Linq;
using System.Reflection;
using Energistics.DataAccess.Reflection;
using PDS.Framework;

namespace PDS.Witsml.Studio.Plugins.ObjectInspector.Models
{
    /// <summary>
    /// Encapsulates the standard family and data schema version of objects to inspect.
    /// </summary>
    public sealed class FamilyVersion : IEquatable<FamilyVersion>
    {
        private static bool _familyVersionsInitialized;
        private static readonly Dictionary<StandardFamily, List<Version>> FamilyVersions = new Dictionary<StandardFamily, List<Version>>();

        /// <summary>
        /// Create a new <see cref="FamilyVersion"/> instance with the specified standard family and data schema version.
        /// </summary>
        /// <param name="standardFamily"></param>
        /// <param name="dataSchemaVersion"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSchemaVersion"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="standardFamily"/> is not an available standard family or <paramref name="dataSchemaVersion"/>
        /// is not an available data schema version for the standard family.</exception>
        public FamilyVersion(StandardFamily standardFamily, Version dataSchemaVersion)
        {
            dataSchemaVersion.NotNull(nameof(dataSchemaVersion));
            if (!IsAvailableStandardFamily(standardFamily))
                throw new ArgumentException(@"Standard family not available", nameof(standardFamily));
            if (!IsAvailableDataSchemaVersion(standardFamily, dataSchemaVersion))
                throw new ArgumentException($"Data schema version not available for {standardFamily.ToString()}", nameof(dataSchemaVersion));

            StandardFamily = standardFamily;
            DataSchemaVersion = dataSchemaVersion;
        }

        /// <summary>
        /// The standard family.
        /// </summary>
        /// <exception cref="ArgumentException">StandardFamily is set to a standard family that is not available.</exception>
        public StandardFamily StandardFamily { get; }

        /// <summary>
        /// The data schema version.
        /// </summary>
        /// <exception cref="InvalidOperationException">DataSchemaVersion is set to a non-null value when StandardFamily is null.</exception>
        /// <exception cref="ArgumentException">DataSchemaVersion is set to a data schema version that is not available for the current standard family.</exception>
        public Version DataSchemaVersion { get; }

        #region Equality and Inequality        
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(FamilyVersion other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return StandardFamily == other.StandardFamily && DataSchemaVersion.Equals(other.DataSchemaVersion);
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
            return Equals((FamilyVersion)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)StandardFamily * 397) ^ DataSchemaVersion.GetHashCode();
            }
        }

        /// <summary>
        /// Checks if two <see cref="FamilyVersion"/> instances are equal to each other.
        /// </summary>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <returns>
        /// true if the <paramref name="left" /> object is equal to the <paramref name="right" /> parameter; otherwise, false.
        /// </returns>
        public static bool operator ==(FamilyVersion left, FamilyVersion right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks if two <see cref="FamilyVersion"/> instances are not equal to each other.
        /// </summary>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <returns>
        /// true if the <paramref name="left" /> object is not equal to the <paramref name="right" /> parameter; otherwise, false.
        /// </returns>
        public static bool operator !=(FamilyVersion left, FamilyVersion right)
        {
            return !Equals(left, right);
        }
        #endregion

        #region Static Properties and Methods
        /// <summary>
        /// Available standards families.
        /// </summary>
        public static IEnumerable<StandardFamily> StandardFamilies
        {
            get
            {
                InitFamilyVersions();

                return FamilyVersions.Keys.OrderBy(sf => sf.ToString());
            }
        }

        /// <summary>
        /// Checks if the requested standard family is present.
        /// </summary>
        /// <param name="standardFamily">The standard family to check.</param>
        /// <returns>True if the standard family is present; false otherwise.</returns>
        public static bool IsAvailableStandardFamily(StandardFamily standardFamily)
        {
            InitFamilyVersions();

            return FamilyVersions.ContainsKey(standardFamily);
        }

        /// <summary>
        /// Checks if the requested data schema version is present in the specified standard family.
        /// </summary>
        /// <param name="standardFamily">The standard family.</param>
        /// <param name="dataSchemaVersion">The data schema version to check.</param>
        /// <returns>True if the standard family is present; false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dataSchemaVersion"/> is null.</exception>
        public static bool IsAvailableDataSchemaVersion(StandardFamily standardFamily, Version dataSchemaVersion)
        {
            dataSchemaVersion.NotNull(nameof(dataSchemaVersion));

            if (!IsAvailableStandardFamily(standardFamily))
                return false;

            return FamilyVersions[standardFamily].Contains(dataSchemaVersion);
        }

        /// <summary>
        /// Gets the available data schema versions for the specified standard family.
        /// </summary>
        /// <param name="standardFamily">The standard family to get data schema versions for.</param>
        /// <returns>The data schema versions for the standard family.</returns>
        /// <exception cref="ArgumentException"><paramref name="standardFamily"/> is not an available standard family.</exception>
        public static IEnumerable<Version> GetDataSchemaVersions(StandardFamily standardFamily)
        {
            if (!IsAvailableStandardFamily(standardFamily))
                throw new ArgumentException(@"Standard family not available", nameof(standardFamily));

            return FamilyVersions[standardFamily];
        }

        /// <summary>
        /// Initializes the mapping from data standard families to available data schema versions.
        /// </summary>
        private static void InitFamilyVersions()
        {
            if (_familyVersionsInitialized) return;

            lock (FamilyVersions)
            {
                if (_familyVersionsInitialized) return;

                _familyVersionsInitialized = true;
                var dataObjectTypes = EnergisticsHelper.GetAllDataObjectTypes().ToList();

                var standardFamilies = dataObjectTypes.Select(t => t.GetCustomAttribute<EnergisticsDataObjectAttribute>().StandardFamily).Distinct();

                foreach (var standardFamily in standardFamilies)
                {
                    var familyTypes = dataObjectTypes.Where(t => t.GetCustomAttribute<EnergisticsDataObjectAttribute>().StandardFamily == standardFamily);
                    var dataSchemaVersions = familyTypes.Select(t => t.GetCustomAttribute<EnergisticsDataObjectAttribute>().DataSchemaVersion).Distinct().OrderBy(v => v.ToString());

                    FamilyVersions.Add(standardFamily, dataSchemaVersions.ToList());
                }
            }
        }
        #endregion
    }
}
