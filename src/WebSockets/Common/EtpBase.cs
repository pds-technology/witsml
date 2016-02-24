using System;
using System.Collections.Generic;
using log4net;

namespace Energistics.Common
{
    public abstract class EtpBase : IDisposable
    {
        public EtpBase()
        {
            Logger = LogManager.GetLogger(GetType());
            RegisteredHandlers = new Dictionary<Type, Type>();
            RegisteredFactories = new Dictionary<Type, Func<object>>();
        }

        public Action<string> Output { get; set; }

        public ILog Logger { get; private set; }

        protected IDictionary<Type, Type> RegisteredHandlers { get; private set; }

        protected IDictionary<Type, Func<object>> RegisteredFactories { get; private set; }

        public string Format(string message, params object[] args)
        {
            return Format(string.Format(message, args));
        }

        public string Format(string message)
        {
            if (Output != null)
            {
                Output(message);
            }

            return message;
        }

        public virtual void Register<TContract, THandler>() where TContract : IProtocolHandler where THandler : TContract
        {
            Register(typeof(TContract), typeof(THandler));
        }

        public virtual void Register<TContract>(Func<TContract> factory) where TContract : IProtocolHandler
        {
            RegisteredFactories[typeof(TContract)] = () => factory();
            Register(typeof(TContract), typeof(TContract));
        }

        protected virtual void Register(Type contractType, Type handlerType)
        {
            RegisteredHandlers[contractType] = handlerType;
        }

        protected virtual void RegisterAll(EtpBase etpBase)
        {
            foreach (var pair in RegisteredFactories)
            {
                etpBase.RegisteredFactories[pair.Key] = pair.Value;
            }

            foreach (var pair in RegisteredHandlers)
            {
                etpBase.Register(pair.Key, pair.Value);
            }
        }

        protected virtual IProtocolHandler CreateInstance(Type contractType)
        {
            if (RegisteredFactories.ContainsKey(contractType))
            {
                return RegisteredFactories[contractType]() as IProtocolHandler;
            }

            var handlerType = RegisteredHandlers[contractType];

            return Activator.CreateInstance(handlerType) as IProtocolHandler;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void CheckDisposed()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).
                    RegisteredHandlers.Clear();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.
                Logger = null;

                disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~EtpBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
