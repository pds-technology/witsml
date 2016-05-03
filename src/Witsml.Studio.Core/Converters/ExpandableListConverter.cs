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
using System.Collections;
using System.ComponentModel;
using PDS.Witsml.Studio.Core.Models;

namespace PDS.Witsml.Studio.Core.Converters
{
    public class ExpandableListConverter : ExpandableObjectConverter
    {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            var list = value as IList;

            if (list == null)
            {
                return base.GetProperties(context, value, attributes);
            }

            var descriptors = new PropertyDescriptorCollection(null);
            var enumerator = list.GetEnumerator();
            var counter = 0;

            while (enumerator.MoveNext())
            {
                descriptors.Add(new ListItemPropertyDescriptor(list, counter++));
            }

            return descriptors;
        }
    }
}
