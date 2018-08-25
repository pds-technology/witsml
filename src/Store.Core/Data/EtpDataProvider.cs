//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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

using Witsml200 = Energistics.DataAccess.WITSML200;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Data provider that implements support for ETP API functions.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.WitsmlDataProvider{TObject}" />
    public abstract class EtpDataProvider<TObject> : WitsmlDataProvider<TObject>
        where TObject : Witsml200.AbstractObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EtpDataProvider{TObject}"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        protected EtpDataProvider(IContainer container, IWitsmlDataAdapter<TObject> dataAdapter) : base(container, dataAdapter)
        {
        }

        /// <summary>
        /// Gets the URI for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        protected override EtpUri GetUri(TObject dataObject)
        {
            return dataObject.GetUri();
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected override void SetDefaultValues(TObject dataObject)
        {
            dataObject.Uuid = dataObject.NewUuid();
            dataObject.Citation = dataObject.Citation.Create();
            dataObject.SchemaVersion = dataObject.SchemaVersion ?? OptionsIn.DataVersion.Version200.Value;
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="uri">The data object URI.</param>
        protected override void SetDefaultValues(TObject dataObject, EtpUri uri)
        {
            dataObject.Uuid = uri.ObjectId;
            dataObject.Citation = dataObject.Citation.Create();
            dataObject.Citation.Title = uri.ObjectId;
        }
    }
}
