using System;
using System.Collections.Generic;

namespace PDS.Framework
{
    public interface IContainer : IDisposable
    {
        void BuildUp(object instance);

        void Register<T>(T instance);

        T Resolve<T>(string contractName = null);

        object Resolve(Type type, string contractName = null);

        IEnumerable<T> ResolveAll<T>(string contractName = null);

        IEnumerable<object> ResolveAll(Type type, string contractName = null);

        //IContainer CreateChildContainer();
    }
}
