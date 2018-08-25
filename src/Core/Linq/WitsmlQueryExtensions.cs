//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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

using System.Linq;
using Energistics.DataAccess;

namespace PDS.WITSMLstudio.Linq
{
    /// <summary>
    /// Provides extension methods for <see cref="IWitsmlQuery{T}"/> instances.
    /// </summary>
    public static class WitsmlQueryExtensions
    {
        /// <summary>
        /// Gets the <see cref="IWitsmlQuery{T}"/> instances by uid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <param name="uid">The uid.</param>
        /// <returns>A data object of the specified type</returns>
        public static T GetByUid<T>(this IWitsmlQuery<T> query, string uid) where T : IDataObject
        {
            return query.Where(x => x.Uid == uid).FirstOrDefault();
        }

        /// <summary>
        /// Gets the <see cref="IWitsmlQuery{T}"/> instances by uid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <param name="uidWell">The uid well.</param>
        /// <param name="uid">The uid.</param>
        /// <returns>A data object of the specified type</returns>
        public static T GetByUid<T>(this IWitsmlQuery<T> query, string uidWell, string uid) where T : IWellObject
        {
            return query.Where(x => x.UidWell == uidWell && x.Uid == uid).FirstOrDefault();
        }

        /// <summary>
        /// Gets the <see cref="IWitsmlQuery{T}"/> instances by uid.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <param name="uidWell">The uid well.</param>
        /// <param name="uidWellbore">The uid wellbore.</param>
        /// <param name="uid">The uid.</param>
        /// <returns>A data object of the specified type</returns>
        public static T GetByUid<T>(this IWitsmlQuery<T> query, string uidWell, string uidWellbore, string uid) where T : IWellboreObject
        {
            return query.Where(x => x.UidWell == uidWell && x.UidWellbore == uidWellbore && x.Uid == uid).FirstOrDefault();
        }

        /// <summary>
        /// Gets the <see cref="IWitsmlQuery{T}"/> instances by name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <param name="name">The name.</param>
        /// <returns>A data object of the specified type</returns>
        public static T GetByName<T>(this IWitsmlQuery<T> query, string name) where T : IDataObject
        {
            return query.Where(x => x.Name == name).FirstOrDefault();
        }

        /// <summary>
        /// Gets the <see cref="IWitsmlQuery{T}"/> instances by name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <param name="nameWell">The name well.</param>
        /// <param name="name">The name.</param>
        /// <returns>A data object of the specified type</returns>
        public static T GetByName<T>(this IWitsmlQuery<T> query, string nameWell, string name) where T : IWellObject
        {
            return query.Where(x => x.NameWell == nameWell && x.Name == name).FirstOrDefault();
        }

        /// <summary>
        /// Gets the <see cref="IWitsmlQuery{T}"/> instances by name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <param name="nameWell">The name well.</param>
        /// <param name="nameWellbore">The name wellbore.</param>
        /// <param name="name">The name.</param>
        /// <returns>A data object of the specified type</returns>
        public static T GetByName<T>(this IWitsmlQuery<T> query, string nameWell, string nameWellbore, string name) where T : IWellboreObject
        {
            return query.Where(x => x.NameWell == nameWell && x.NameWellbore == nameWellbore && x.Name == name).FirstOrDefault();
        }
    }
}
