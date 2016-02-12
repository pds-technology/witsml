using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;

namespace PDS.Framework.Web
{
    public class DependencyResolver : IDependencyResolver, System.Web.Mvc.IDependencyResolver
    {
        private readonly IContainer _container;

        public DependencyResolver(IContainer container)
        {
            _container = container;
        }

        public IDependencyScope BeginScope()
        {
            //var container = _Container.CreateChildContainer();
            //return new DependencyResolver(container);
            return this;
        }

        public object GetService(Type serviceType)
        {
            return _container.Resolve(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _container.ResolveAll(serviceType);
        }

        public void Dispose()
        {
            if (_container != null)
            {
                _container.Dispose();
            }
        }
    }
}