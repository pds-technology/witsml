using System.ServiceModel;
using System.Web.Http.Dependencies;

namespace PDS.Framework.Web.Services
{
    public class ServiceInstanceContextExtension : IExtension<InstanceContext>
    {
        private IDependencyScope _scope;

        public void Attach(InstanceContext owner)
        {
        }

        public void Detach(InstanceContext owner)
        {
        }

        public IDependencyScope GetChildScope(IDependencyResolver resolver)
        {
            if (_scope == null)
            {
                _scope = resolver.BeginScope();
            }

            return _scope;
        }

        public void DisposeChildScope()
        {
            if (_scope != null)
            {
                _scope.Dispose();
            }
        }
    }
}
