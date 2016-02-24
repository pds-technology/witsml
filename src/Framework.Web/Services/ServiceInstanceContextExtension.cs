using System.ServiceModel;
using System.Web.Http.Dependencies;

namespace PDS.Framework.Web.Services
{
    /// <summary>
    /// Service extension that provides access to the composition container used for dependency injection.
    /// </summary>
    /// <seealso cref="System.ServiceModel.IExtension{System.ServiceModel.InstanceContext}" />
    public class ServiceInstanceContextExtension : IExtension<InstanceContext>
    {
        private IDependencyScope _scope;

        /// <summary>
        /// Enables an extension object to find out when it has been aggregated. Called when the extension is 
        /// added to the <see cref="P:System.ServiceModel.IExtensibleObject`1.Extensions" /> property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Attach(InstanceContext owner)
        {
        }

        /// <summary>
        /// Enables an object to find out when it is no longer aggregated. Called when an extension is 
        /// removed from the <see cref="P:System.ServiceModel.IExtensibleObject`1.Extensions" /> property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Detach(InstanceContext owner)
        {
        }

        /// <summary>
        /// Gets the child scope.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <returns>The child scope.</returns>
        public IDependencyScope GetChildScope(IDependencyResolver resolver)
        {
            if (_scope == null)
            {
                _scope = resolver.BeginScope();
            }

            return _scope;
        }

        /// <summary>
        /// Disposes the child scope.
        /// </summary>
        public void DisposeChildScope()
        {
            if (_scope != null)
            {
                _scope.Dispose();
            }
        }
    }
}
