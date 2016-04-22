﻿//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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

using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.Datatypes;
using PDS.Framework;

namespace PDS.Witsml.Server.Data
{
    public abstract class EtpDataProvider<TObject> : WitsmlDataProvider<TObject>
        where TObject : AbstractObject
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
        }
    }
}