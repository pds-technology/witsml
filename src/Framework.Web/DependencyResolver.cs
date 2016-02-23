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
            // DO NOT dispose IContainer!
        }
    }
}