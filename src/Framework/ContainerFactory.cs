using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace PDS.Framework
{
    public static class ContainerFactory
    {
        public static IContainer Create(string assemblyPath = ".")
        {
            var catalog = new AggregateCatalog
            (
                new DirectoryCatalog(assemblyPath, "PDS.*.dll")
            );

            return Create(catalog);
        }

        public static IContainer Create(ComposablePartCatalog catalog)
        {
            var container = new CompositionContainer(catalog);
            var instance = new Container(container);

            container.ComposeExportedValue<IContainer>(instance);

            return instance;
        }
    }
}
