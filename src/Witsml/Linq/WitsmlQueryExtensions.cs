//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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

using System.Linq;
using Energistics.DataAccess;

namespace PDS.Witsml.Linq
{
    public static class WitsmlQueryExtensions
    {
        public static T GetByUid<T>(this IWitsmlQuery<T> query, string uid) where T : IDataObject
        {
            return query.FirstOrDefault(x => x.Uid == uid);
        }

        public static T GetByUid<T>(this IWitsmlQuery<T> query, string uidWell, string uid) where T : IWellObject
        {
            return query.FirstOrDefault(x => x.UidWell == uidWell && x.Uid == uid);
        }

        public static T GetByUid<T>(this IWitsmlQuery<T> query, string uidWell, string uidWellbore, string uid) where T : IWellboreObject
        {
            return query.FirstOrDefault(x => x.UidWell == uidWell && x.UidWellbore == uidWellbore && x.Uid == uid);
        }

        public static T GetByName<T>(this IWitsmlQuery<T> query, string name) where T : IDataObject
        {
            return query.FirstOrDefault(x => x.Name == name);
        }

        public static T GetByName<T>(this IWitsmlQuery<T> query, string nameWell, string name) where T : IWellObject
        {
            return query.FirstOrDefault(x => x.NameWell == nameWell && x.Name == name);
        }

        public static T GetByName<T>(this IWitsmlQuery<T> query, string nameWell, string nameWellbore, string name) where T : IWellboreObject
        {
            return query.FirstOrDefault(x => x.NameWell == nameWell && x.NameWellbore == nameWellbore && x.Name == name);
        }
    }
}
