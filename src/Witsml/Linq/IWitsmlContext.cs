//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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
using Energistics.DataAccess;
using Energistics.Datatypes;

namespace PDS.Witsml.Linq
{
    public interface IWitsmlContext
    {
        Action<Functions, string, string> LogQuery { get; set; }

        Action<Functions, string, string, string, short, string> LogResponse { get; set; }

        IEnumerable<IDataObject> GetAllWells();

        IEnumerable<IWellObject> GetWellbores(EtpUri parentUri);

        IEnumerable<IWellboreObject> GetWellboreObjects(string objectType, EtpUri parentUri);

        IWellboreObject GetGrowingObjectHeaderOnly(string objectType, EtpUri uri);

        IDataObject GetObjectIdOnly(string objectType, EtpUri uri);

        IDataObject GetObjectDetails(string objectType, EtpUri uri);
    }
}
