using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Web.Http.Dependencies;

namespace PDS.Framework.Web.Services
{
    public class ServiceInstanceProvider : IInstanceProvider
    {
        private readonly IDependencyResolver _resolver;
        private readonly Type _contractType;

        public ServiceInstanceProvider(IDependencyResolver resolver, Type contractType)
        {
            _resolver = resolver;
            _contractType = contractType;
        }

        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            //var scope = instanceContext.Extensions.Find<ServiceInstanceContextExtension>().GetChildScope(_Resolver);
            //return scope.GetService(_ContractType);
            return _resolver.GetService(_contractType);
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            return GetInstance(instanceContext, null);
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            //instanceContext.Extensions.Find<ServiceInstanceContextExtension>().DisposeChildScope();
        }
    }
}