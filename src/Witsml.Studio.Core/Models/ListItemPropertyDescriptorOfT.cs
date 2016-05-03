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
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PDS.Witsml.Studio.Core.Models
{
    public class ListItemPropertyDescriptor<T> : PropertyDescriptor
    {
        private readonly IList<T> _owner;
        private readonly int _index;

        public ListItemPropertyDescriptor(IList<T> owner, int index) : base("[" + index + "]", null)
        {
            _owner = owner;
            _index = index;

        }

        public override AttributeCollection Attributes
        {
            get
            {
                var attributes = TypeDescriptor.GetAttributes(GetValue(null), false);

                // If the Xceed expandable object attribute is not applied then apply it
                if (!attributes.OfType<ExpandableObjectAttribute>().Any())
                {
                    attributes = AddAttribute(new ExpandableObjectAttribute(), attributes);
                }

                // Set the xceed order attribute
                attributes = AddAttribute(new PropertyOrderAttribute(_index), attributes);

                return attributes;
            }
        }
        public override Type PropertyType => Value?.GetType();

        public override Type ComponentType => _owner.GetType();

        public override bool IsReadOnly => false;

        private T Value => _owner[_index];

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override object GetValue(object component)
        {
            return Value;
        }

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object component, object value)
        {
            _owner[_index] = (T)value;
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        private AttributeCollection AddAttribute(Attribute newAttribute, AttributeCollection oldAttributes)
        {
            var newAttributes = new Attribute[oldAttributes.Count + 1];
            oldAttributes.CopyTo(newAttributes, 1);
            newAttributes[0] = newAttribute;

            return new AttributeCollection(newAttributes);
        }
    }
}
