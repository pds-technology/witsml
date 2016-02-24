using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace PDS.Framework.Web.Services
{
    /// <summary>
    /// Service extension initializer implementation.
    /// </summary>
    /// <seealso cref="System.ServiceModel.Dispatcher.IInstanceContextInitializer" />
    public class ServiceInstanceContextInitializer : IInstanceContextInitializer
    {
        /// <summary>
        /// Provides the ability to modify the newly created <see cref="T:System.ServiceModel.InstanceContext" /> object.
        /// </summary>
        /// <param name="instanceContext">The system-supplied instance context.</param>
        /// <param name="message">The message that triggered the creation of the instance context.</param>
        public void Initialize(InstanceContext instanceContext, Message message)
        {
            instanceContext.Extensions.Add(new ServiceInstanceContextExtension());
        }
    }
}
