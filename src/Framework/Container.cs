//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Provides access to the composition container used for dependency injection.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Framework.IContainer" />
    public class Container : IContainer
    {
        private readonly CompositionContainer _container;

        /// <summary>
        /// Initializes a new instance of the <see cref="Container"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        internal Container(CompositionContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Satisfies all registered dependencies for the specified object instance.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        public void BuildUp(object instance)
        {
            _container.SatisfyImportsOnce(instance);
        }

        /// <summary>
        /// Registers the specified instance for dependency injection.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <param name="instance">The object instance.</param>
        /// <param name="contractName">The contract name.</param>
        public void Register<T>(T instance, string contractName = null)
        {
            if (string.IsNullOrWhiteSpace(contractName))
                _container.ComposeExportedValue<T>(instance);
            else
                _container.ComposeExportedValue<T>(contractName, instance);
        }

        /// <summary>
        /// Resolves a single instance of the specified type and optional contract name.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <param name="contractName">The contract name.</param>
        /// <returns>The object instance with all dependencies resolved.</returns>
        /// <exception cref="ContainerException">Error resolving contract type or contract name</exception>
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

        /// <summary>
        /// Resolves a single instance of the specified type and optional contract name.
        /// </summary>
        /// <param name="type">The contract type.</param>
        /// <param name="contractName">The contract name.</param>
        /// <returns>The object instance with all dependencies resolved.</returns>
        public object Resolve(Type type, string contractName = null)
        {
            return ResolveAll(type, contractName)
                .FirstOrDefault();
        }

        /// <summary>
        /// Resolves all instances of the specified type and optional contract name.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <param name="contractName">The contract name.</param>
        /// <returns>A collection of object instances with all dependencies resolved.</returns>
        /// <exception cref="ContainerException">Error resolving contract type or contract name</exception>
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

        /// <summary>
        /// Resolves all instances of the specified type and optional contract name.
        /// </summary>
        /// <param name="type">The contract type.</param>
        /// <param name="contractName">The contract name.</param>
        /// <returns>A collection of object instances with all dependencies resolved.</returns>
        /// <exception cref="ContainerException">Error resolving contract type or contract name</exception>
        public IEnumerable<object> ResolveAll(Type type, string contractName = null)
        {
            if (type == null && string.IsNullOrWhiteSpace(contractName))
                return Enumerable.Empty<object>();

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

        /// <summary>
        /// Tries to resolve a single instance of the specified type and optional contract name.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <param name="contractName">The contract name.</param>
        /// <returns>The object instance with all dependencies resolved.</returns>
        public T TryResolve<T>(string contractName = null)
        {
            try
            {
                return Resolve<T>(contractName);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Tries to resolve a single instance of the specified type and optional contract name.
        /// </summary>
        /// <param name="type">The contract type.</param>
        /// <param name="contractName">The contract name.</param>
        /// <returns>The object instance with all dependencies resolved.</returns>
        public object TryResolve(Type type, string contractName = null)
        {
            try
            {
                return Resolve(type, contractName);
            }
            catch
            {
                return null;
            }
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).
                    _container?.Dispose();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.

                disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Container() {
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
