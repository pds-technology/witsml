using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace PDS.Framework
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
            var container = new CompositionContainer(catalog);
            var instance = new Container(container);

            container.ComposeExportedValue<IContainer>(instance);

            return instance;
        }
    }
}
