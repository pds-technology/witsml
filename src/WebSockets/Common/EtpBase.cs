//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Energistics.Datatypes;
using log4net;

namespace Energistics.Common
{
    /// <summary>
    /// Provides common functionality for ETP session and protocol handler implementations.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public abstract class EtpBase : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EtpBase"/> class.
        /// </summary>
        protected EtpBase()
        {
            Logger = LogManager.GetLogger(GetType());
            SupportedObjects = new List<string>();
            RegisteredHandlers = new Dictionary<Type, Type>();
            RegisteredFactories = new Dictionary<Type, Func<object>>();
        }

        /// <summary>
        /// Gets or sets a delegate to process logging messages.
        /// </summary>
        /// <value>The output delegate.</value>
        public Action<string> Output { get; set; }

        /// <summary>
        /// Gets the logger used by this instance.
        /// </summary>
        /// <value>The logger instance.</value>
        public ILog Logger { get; private set; }

        /// <summary>
        /// Gets or sets the list of supported objects.
        /// </summary>
        /// <value>The supported objects.</value>
        public IList<string> SupportedObjects { get; set; }

        /// <summary>
        /// Gets the collection of registered protocol handlers.
        /// </summary>
        /// <value>The registered protocol handlers.</value>
        protected IDictionary<Type, Type> RegisteredHandlers { get; }

        /// <summary>
        /// Gets the collection of registered protocol handler factories.
        /// </summary>
        /// <value>The registered protocol handler factories.</value>
        protected IDictionary<Type, Func<object>> RegisteredFactories { get; }

        /// <summary>
        /// Creates a dictionary containing an Authorization header for the specified username and password.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A dictionary containing an Authorization header.</returns>
        public static IDictionary<string, string> Authorization(string username, string password)
        {
            return GetAuthorizationHeader("Basic", string.IsNullOrWhiteSpace(username) ? string.Empty : string.Concat(username, ":", password));
        }

        /// <summary>
        /// Creates a dictionary containing an Authorization header for the specified JSON web token.
        /// </summary>
        /// <param name="token">The JSON web token.</param>
        /// <returns>A dictionary containing an Authorization header.</returns>
        public static IDictionary<string, string> Authorization(string token)
        {
            return GetAuthorizationHeader("Bearer", string.IsNullOrWhiteSpace(token) ? string.Empty : token);
        }

        /// <summary>
        /// Creates a dictionary containing an Authorization header for the specified schema and encoded string.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <param name="encodedString">The encoded string.</param>
        /// <returns>A dictionary containing an Authorization header.</returns>
        private static IDictionary<string, string> GetAuthorizationHeader(string schema, string encodedString)
        {
            var encoded = Convert.ToBase64String(Encoding.Default.GetBytes(encodedString));
            var headers = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(encoded))
            {
                headers["Authorization"] = string.Concat(schema, " ", encoded);
            }

            return headers;
        }

        /// <summary>
        /// Formats the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The formatted message.</returns>
        public string Format(string message, params object[] args)
        {
            return Format(string.Format(message, args));
        }

        /// <summary>
        /// Formats the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public string Format(string message)
        {
            Output?.Invoke(message);
            return message;
        }

        /// <summary>
        /// Called when the ETP session is opened.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public virtual void OnSessionOpened(IList<SupportedProtocol> supportedProtocols)
        {
        }

        /// <summary>
        /// Registers a protocol handler for the specified contract type.
        /// </summary>
        /// <typeparam name="TContract">The type of the contract.</typeparam>
        /// <typeparam name="THandler">The type of the handler.</typeparam>
        public virtual void Register<TContract, THandler>() where TContract : IProtocolHandler where THandler : TContract
        {
            Register(typeof(TContract), typeof(THandler));
        }

        /// <summary>
        /// Registers a protocol handler factory for the specified contract type.
        /// </summary>
        /// <typeparam name="TContract">The type of the contract.</typeparam>
        /// <param name="factory">The factory.</param>
        public virtual void Register<TContract>(Func<TContract> factory) where TContract : IProtocolHandler
        {
            RegisteredFactories[typeof(TContract)] = () => factory();
            Register(typeof(TContract), typeof(TContract));
        }

        /// <summary>
        /// Registers a protocol handler for the specified contract type.
        /// </summary>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="handlerType">Type of the handler.</param>
        protected virtual void Register(Type contractType, Type handlerType)
        {
            RegisteredHandlers[contractType] = handlerType;
        }

        /// <summary>
        /// Registers all protocol handlers and factories for the specified <see cref="EtpBase"/>.
        /// </summary>
        /// <param name="etpBase">The ETP base instance.</param>
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

        /// <summary>
        /// Creates an instance of the specified protocol handler.
        /// </summary>
        /// <param name="contractType">Type of the contract.</param>
        /// <returns>The protocol handler instance.</returns>
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

        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Checks whether the current instance has been disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected virtual void CheckDisposed()
        {
            if (_disposedValue)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).
                    RegisteredHandlers.Clear();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.
                Logger = null;

                _disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~EtpBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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
