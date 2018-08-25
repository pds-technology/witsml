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
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141;
using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.v11.Datatypes.Object;
using Energistics.Etp.v11.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    /// <summary>
    /// Well141EtpTests
    /// </summary>
    public partial class Well141EtpTests
    {
        [TestMethod]
        public async Task Well141_GetResources_Can_Get_Root_Level_Resources()
        {
            AddParents();
            DevKit.AddAndAssert<WellList, Well>(Well);

            await RequestSessionAndAssert();
            await GetResourcesAndAssert(new EtpUri(EtpUri.RootUri));
        }

        [TestMethod]
        public async Task Well141_GetResources_Can_Detect_Invalid_Uris()
        {
            await RequestSessionAndAssert();

            await GetResourcesAndAssert(new EtpUri("eml://unknown141"), EtpErrorCodes.InvalidUri);
            await GetResourcesAndAssert(new EtpUri("eml://witsml141/ChannelSet"));
            await GetResourcesAndAssert(new EtpUri("eml://witsml141"));
        }

        [TestMethod]
        public async Task Well141_GetObject_Can_Detect_Invalid_Uris()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();

            // Get Invalid Object
            await GetAndAssert(handler, new EtpUri("eml://unknown141/wellz(123)"), EtpErrorCodes.InvalidUri);
            await GetAndAssert(handler, new EtpUri("eml://unknown141/well"), EtpErrorCodes.InvalidUri);
            await GetAndAssert(handler, new EtpUri("eml://witsml141/ChannelSet(123)"), EtpErrorCodes.UnsupportedObject);
            await GetAndAssert(handler, new EtpUri("eml://witsml141"), EtpErrorCodes.UnsupportedObject);
        }

        [TestMethod]
        public async Task Well141_DeleteObject_Can_Detect_Invalid_Uris()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();

            // Delete Invalid Object
            await DeleteAndAssert(handler, new EtpUri("eml://unknown141/wellz(123)"), EtpErrorCodes.InvalidUri);
            await DeleteAndAssert(handler, new EtpUri("eml://witsml141/ChannelSet"), EtpErrorCodes.UnsupportedObject);
            await DeleteAndAssert(handler, new EtpUri("eml://witsml141"), EtpErrorCodes.UnsupportedObject);
        }

        [TestMethod]
        public async Task Well141_PutObject_Can_Detect_Invalid_Data_Objects()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();

            // Put Invalid Object
            await PutAndAssert(handler, ToDataObject(EtpUris.Witsml141, BasicXMLTemplate), EtpErrorCodes.InvalidObject);
            await PutAndAssert(handler, ToDataObject("eml://unknown141/wellz(123)"), EtpErrorCodes.InvalidUri);
            await PutAndAssert(handler, ToDataObject("eml://witsml141/ChannelSet"), EtpErrorCodes.UnsupportedObject);
            await PutAndAssert(handler, ToDataObject("eml://witsml141"), EtpErrorCodes.UnsupportedObject);
        }

        private DataObject ToDataObject(string uri, string templateXml = null)
        {
            var uuid = DevKit.Uid();

            var dataObject = new DataObject
            {
                Resource = new Resource
                {
                    ContentType = EtpContentTypes.Witsml141,
                    ResourceType = ResourceTypes.DataObject.ToString(),
                    CustomData = new Dictionary<string, string>(),
                    Uuid = DevKit.Uid(),
                    Name = DevKit.Name(),
                    HasChildren = -1,
                    Uri = uri
                },
                ContentEncoding = string.Empty,
                Data = new byte[0]
            };

            if (!string.IsNullOrWhiteSpace(templateXml))
            {
                // Update data object XML
                var xml = string.Format(templateXml, uuid, string.Empty);
                dataObject.SetString(xml);

                // Update resource URI
                dataObject.Resource.Uri = EtpUris.Witsml141.Append(ObjectTypes.Well, uuid);
            }

            return dataObject;
        }

        [TestMethod]
        public async Task Well141_DeleteObject_Cannot_Delete_Well_With_Child_Wellbore()
        {
            AddParents();
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = Well.GetUri();

            var dataObject = CreateDataObject<WellList, Well>(uri, Well);

            // Put Object
            await PutAndAssert(handler, dataObject);

            var wellbore = new Wellbore()
            {
                UidWell = Well.Uid,
                Uid = DevKit.Uid(),
                NameWell = Well.Name,
                Name = DevKit.Name("Wellbore")
            };

            var wellboreObject = CreateDataObject<WellboreList, Wellbore>(wellbore.GetUri(), wellbore);

            // Put Wellbore
            await PutAndAssert(handler, wellboreObject);

            // Delete Well
            await DeleteAndAssert(handler, uri, EtpErrorCodes.NoCascadeDelete);

            // Delete Wellbore
            await DeleteAndAssert(handler, wellbore.GetUri());

            // Delete Well
            await DeleteAndAssert(handler, uri);
        }
    }
}
