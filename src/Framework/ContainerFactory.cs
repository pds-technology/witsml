//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2017.2
//
// Copyright 2017 PDS Americas LLC
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

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using PDS.WITSMLstudio.Framework.Properties;

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Provides methods to create an instance of the composition container.
    /// </summary>
    public static class ContainerFactory
    {
        private static readonly string _defaultAssemblySearchPattern = Settings.Default.DefaultAssemblySearchPattern;

        /// <summary>
        /// Creates a composition container using the specified assembly path.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <returns>The composition container instance.</returns>
        public static IContainer Create(string assemblyPath = ".")
        {
            var catalog = new AggregateCatalog
            (
                new DirectoryCatalog(assemblyPath, _defaultAssemblySearchPattern)
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
