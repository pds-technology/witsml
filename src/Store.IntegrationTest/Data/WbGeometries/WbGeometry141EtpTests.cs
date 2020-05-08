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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.v11.Protocol;
using Energistics.Etp.v11.Protocol.Core;
using Energistics.Etp.v11.Protocol.Discovery;
using Energistics.Etp.v11.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WbGeometry = Energistics.DataAccess.WITSML141.StandAloneWellboreGeometry;
using WbGeometryList = Energistics.DataAccess.WITSML141.WellboreGeometryList;

using System.Diagnostics;

namespace PDS.WITSMLstudio.Store.Data.WbGeometries
{
    /// <summary>
    /// WbGeometry141EtpTests
    /// </summary>
    public partial class WbGeometry141EtpTests
    {
        [TestMethod]
        public async Task WbGeometry141_GetResources_Can_Get_All_WbGeometry_Resources_2()
        {
            AddParents();
            DevKit.AddAndAssert<WbGeometryList, WbGeometry>(WbGeometry);
            {
                var actual = await RequestSessionAndAssert();
                Assert.IsNotNull(actual);
            }

            var uri = WbGeometry.GetUri();
            var parentUri = uri.Parent;

            {
                var actual = await GetResourcesAndAssert(parentUri);
                Assert.IsNotNull(actual);
                Assert.IsTrue(actual.Any());
            }

            {
                var folderUri = parentUri.Append(uri.ObjectType);
                var actual = await GetResourcesAndAssert(folderUri);
                Assert.IsNotNull(actual);
                Assert.IsTrue(actual.Any());
            }
        }

        [TestMethod]
        public void WbGeometry141_GetResources_IsValidUri()
        {
            var providers = DevKit.Container.ResolveAll<PDS.WITSMLstudio.Store.Providers.Discovery.IDiscoveryStoreProvider>();
            Assert.IsNotNull(providers);

            var list = providers.ToList();
            Assert.IsTrue(list.Any());

            foreach (var provider in list)
                Debug.WriteLine(provider.DataSchemaVersion);

            var actual = list.FirstOrDefault(x => x.DataSchemaVersion == "1.4.1.1") as Providers.Discovery.Witsml141Provider;
            Assert.IsNotNull(actual);

            var access = new PrivateObject(actual);

            var uri = WbGeometry.GetUri();
            var parentUri = uri.Parent;
            var folderUri = parentUri.Append(uri.ObjectType);

            bool valid = (bool)access.Invoke("IsValidUri", folderUri);
            Assert.IsTrue(valid);
        }
    }
}
