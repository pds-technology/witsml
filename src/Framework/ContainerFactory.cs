//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Provides methods to create an instance of the composition container.
    /// </summary>
    public static class ContainerFactory
    {
        /// <summary>
        /// Creates a composition container using the specified assembly path.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <returns>The composition container instance.</returns>
        public static IContainer Create(string assemblyPath = ".")
        {
            var catalog = new AggregateCatalog
            (
                new DirectoryCatalog(assemblyPath, "PDS.*.dll")
            );

            return Create(catalog);
        }

        /// <summary>
        /// Creates a composition container using the specified catalog.
        /// </summary>
        /// <param name="catalog">The catalog.</param>
        /// <returns>A composition container instance.</returns>
        public static IContainer Create(ComposablePartCatalog catalog)
        {
            var container = new CompositionContainer(catalog, true);
            var instance = new Container(container);

            container.ComposeExportedValue<IContainer>(instance);

            return instance;
        }
    }
}
