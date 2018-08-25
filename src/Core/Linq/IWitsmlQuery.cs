//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.Linq;

namespace PDS.WITSMLstudio.Linq
{
    /// <summary>
    /// Defines the properties and methods for a WITSML query.
    /// </summary>
    /// <seealso cref="System.Linq.IQueryable" />
    public interface IWitsmlQuery : IQueryable
    {
        /// <summary>
        /// Provides a callback that can be used to include specific elements in the query response.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IWitsmlQuery Include(Action<object> action);

        /// <summary>
        /// Sets the options that will be passed in to the GetFromStore query.
        /// </summary>
        /// <param name="optionsIn"></param>
        /// <returns></returns>
        IWitsmlQuery With(OptionsIn optionsIn);
    }

    /// <summary>
    /// Defines the properties and methods for a type specific WITSML query.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Linq.IQueryable" />
    public interface IWitsmlQuery<T> : IQueryable<T>
    {
        /// <summary>
        /// Provides a callback that can be used to include specific elements in the query response.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IWitsmlQuery<T> Include(Action<T> action);

        /// <summary>
        /// Sets the options that will be passed in to the GetFromStore query.
        /// </summary>
        /// <param name="optionsIn"></param>
        /// <returns></returns>
        IWitsmlQuery<T> With(OptionsIn optionsIn);
    }
}
