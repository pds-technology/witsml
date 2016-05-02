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
using System.Data;
using System.Linq;
using PDS.Framework;

namespace PDS.Witsml.Server.Models
{
    public static class ModelExtensions
    {
        public static ColumnMapping GetColumn(this ObjectMapping mapping, string alias)
        {
            return mapping?.Columns
                .FirstOrDefault(x => alias.EqualsIgnoreCase(x.Alias));
        }

        public static string GetName(this ColumnMapping column)
        {
            if (!string.IsNullOrWhiteSpace(column.Alias))
                return column.Alias;

            return column.Column.Contains('.')
                ? column.Column.Split('.').Last()
                : column.Column;
        }

        public static ExtensionMapping GetExtension(this ColumnMapping mapping, string key)
        {
            return mapping?.Extensions
                .Where(x => x.Key.EqualsIgnoreCase(key))
                .Select(x => x.Value ?? new ExtensionMapping())
                .ForEach(x =>
                {
                    if (string.IsNullOrWhiteSpace(x.Value) && string.IsNullOrWhiteSpace(x.Alias))
                    {
                        x.Alias = key;
                    }
                })
                .FirstOrDefault();
        }

        public static object GetValue(this ExtensionMapping mapping, IDataReader reader)
        {
            if (mapping == null) return null;
            if (mapping.Value != null) return mapping.Value;

            if (mapping.Alias != null && reader != null)
                return reader.GetValue(reader.GetOrdinal(mapping.Alias));

            return null;
        }

        public static T GetValue<T>(this ExtensionMapping mapping, IDataReader reader) where T : IConvertible
        {
            var value = GetValue(mapping, reader);
            if (value == null) return default(T);

            return (T) Convert.ChangeType(value, typeof(T));
        }

        public static string GetString(this ExtensionMapping mapping, IDataReader reader)
        {
            var value = GetValue(mapping, reader);
            return value?.ToString();
        }
    }
}
