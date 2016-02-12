using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;

namespace PDS.Framework
{
    public class Container : IContainer
    {
        private CompositionContainer _container;

        internal Container(CompositionContainer container)
        {
            _container = container;
        }

        public void BuildUp(object instance)
        {
            _container.SatisfyImportsOnce(instance);
        }

        public void Register<T>(T instance)
        {
            _container.ComposeExportedValue<T>(instance);
        }

        public T Resolve<T>(string contractName = null)
        {
            try
            {
                return _container.GetExportedValue<T>(contractName);
            }
            catch (Exception ex)
            {
                throw new ContainerException("Error resolving type: " + typeof(T).FullName + " and contract name: \"" + contractName + "\"", ex);
            }
        }

        public object Resolve(Type type, string contractName = null)
        {
            return ResolveAll(type, contractName)
                .FirstOrDefault();
        }

        public IEnumerable<T> ResolveAll<T>(string contractName = null)
        {
            try
            {
                return _container.GetExportedValues<T>(contractName);
            }
            catch (Exception ex)
            {
                throw new ContainerException("Error resolving all of type: " + typeof(T).FullName + " and contract name: \"" + contractName + "\"", ex);
            }
        }

        public IEnumerable<object> ResolveAll(Type type, string contractName = null)
        {
            try
            {
                return _container.GetExports(type, null, contractName)
                    .Select(x => x.Value);
            }
            catch (Exception ex)
            {
                throw new ContainerException("Error resolving all of type: " + type.FullName + " and contract name: " + (contractName ?? "(none)"), ex);
            }
        }

        //public IContainer CreateChildContainer()
        //{
        //    return new Container(new CompositionContainer(_Container));
        //}

        public void Dispose()
        {
            if (_container != null)
            {
                _container.Dispose();
            }
        }
    }
}
