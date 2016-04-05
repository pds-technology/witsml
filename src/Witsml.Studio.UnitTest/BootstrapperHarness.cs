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
using Caliburn.Micro;

namespace PDS.Witsml.Studio
{
    /// <summary>
    /// Exposes protected bootstrapper methods for unit testing.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Studio.Bootstrapper" />
    public class BootstrapperHarness : Bootstrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BootstrapperHarness"/> class.
        /// </summary>
        public BootstrapperHarness() : base(false)
        {
            AssemblySource.Instance.Clear();
        }

        /// <summary>
        /// Exposes the SelectAssemblies method.
        /// </summary>
        /// <returns>An IEnumerable of Assemblies</returns>
        public IEnumerable<Assembly> CallSelectAssemblies()
        {
            return SelectAssemblies();
        }

        /// <summary>
        /// Exposes the GetInstance method.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>The instance for the given objectType</returns>
        public object CallGetInstance(Type objectType)
        {
            return GetInstance(objectType, null);
        }

        /// <summary>
        /// Exposes the OnStartup method.
        /// </summary>
        public void CallOnStartup()
        {
            OnStartup(null, null);
        }

        /// <summary>
        /// Overrides the SelectAssemblies() to include the assembly for the unit tests.
        /// </summary>
        /// <returns>
        /// An IEnumerable of the Assemblies found in the Plugins folder and the unit test assembly.
        /// </returns>
        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            return base.SelectAssemblies()
                .Union(new[] { GetType().Assembly });
        }
    }
}
