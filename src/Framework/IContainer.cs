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

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Defines methods that can be used to manage dependencies between objects.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IContainer : IDisposable
    {
        /// <summary>
        /// Gets the composition container configuration.
        /// </summary>
        ContainerConfig Configuration { get; }

        /// <summary>
        /// Satisfies all registered dependencies for the specified object instance.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        void BuildUp(object instance);

        /// <summary>
        /// Registers the specified instance for dependency injection.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <param name="instance">The object instance.</param>
        /// <param name="contractName">The contract name.</param>
        void Register<T>(T instance, string contractName = null);

        /// <summary>
        /// Resolves a single instance of the specified type and optional contract name.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <param name="contractName">The contract name.</param>
        /// <returns>The object instance with all dependencies resolved.</returns>
        T Resolve<T>(string contractName = null);

        /// <summary>
        /// Resolves a single instance of the specified type and optional contract name.
        /// </summary>
        /// <param name="type">The contract type.</param>
        /// <param name="contractName">The contract name.</param>
        /// <returns>The object instance with all dependencies resolved.</returns>
        object Resolve(Type type, string contractName = null);

        /// <summary>
        /// Resolves all instances of the specified type and optional contract name.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <param name="contractName">The contract name.</param>
        /// <returns>A collection of object instances with all dependencies resolved.</returns>
        IEnumerable<T> ResolveAll<T>(string contractName = null);

        /// <summary>
        /// Resolves all instances of the specified type and optional contract name.
        /// </summary>
        /// <param name="type">The contract type.</param>
        /// <param name="contractName">The contract name.</param>
        /// <returns>A collection of object instances with all dependencies resolved.</returns>
        IEnumerable<object> ResolveAll(Type type, string contractName = null);

        /// <summary>
        /// Tries to resolve a single instance of the specified type and optional contract name.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <param name="contractName">The contract name.</param>
        /// <returns>The object instance with all dependencies resolved.</returns>
        T TryResolve<T>(string contractName = null);

        /// <summary>
        /// Tries to resolve a single instance of the specified type and optional contract name.
        /// </summary>
        /// <param name="type">The contract type.</param>
        /// <param name="contractName">The contract name.</param>
        /// <returns>The object instance with all dependencies resolved.</returns>
        object TryResolve(Type type, string contractName = null);
    }
}
