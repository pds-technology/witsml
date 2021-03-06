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

// ----------------------------------------------------------------------
// <auto-generated>
//     Changes to this file may cause incorrect behavior and will be lost
//     if the code is regenerated.
// </auto-generated>
// ----------------------------------------------------------------------

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

namespace PDS.WITSMLstudio.Store.Data.DepthRegImages
{
    [TestClass]
    public partial class DepthRegImage141EtpTests : DepthRegImage141TestBase
    {
        public DepthRegImage141EtpTests()
            : base(true)
        {
        }

        [TestMethod]
        public void DepthRegImage141_Ensure_Creates_DepthRegImage_With_Default_Values()
        {
            DevKit.EnsureAndAssert<DepthRegImageList, DepthRegImage>(DepthRegImage);
        }

        [TestMethod]
        public async Task DepthRegImage141_GetResources_Can_Get_All_DepthRegImage_Resources()
        {
            AddParents();
            DevKit.AddAndAssert<DepthRegImageList, DepthRegImage>(DepthRegImage);
            await RequestSessionAndAssert();

            var uri = DepthRegImage.GetUri();
            var parentUri = uri.Parent;

            await GetResourcesAndAssert(parentUri);

            var folderUri = parentUri.Append(uri.ObjectType);
            await GetResourcesAndAssert(folderUri);
        }

        [TestMethod]
        public async Task DepthRegImage141_PutObject_Can_Add_DepthRegImage()
        {
            AddParents();
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = DepthRegImage.GetUri();

            var dataObject = CreateDataObject<DepthRegImageList, DepthRegImage>(uri, DepthRegImage);

            // Get Object Expecting it Not to Exist
            await GetAndAssert(handler, uri, Energistics.Etp.EtpErrorCodes.NotFound);

            // Put Object
            await PutAndAssert(handler, dataObject);

            // Get Object
            var args = await GetAndAssert(handler, uri);

            // Check Data Object XML
            Assert.IsNotNull(args?.Message.DataObject);
            var xml = args.Message.DataObject.GetString();

            var result = Parse<DepthRegImageList, DepthRegImage>(xml);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task DepthRegImage141_PutObject_Can_Update_DepthRegImage()
        {
            AddParents();
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = DepthRegImage.GetUri();

            // Add a Comment to Data Object            
            DepthRegImage.CommonData = new CommonData()
            {
                Comments = "Test PutObject"
            };

            var dataObject = CreateDataObject<DepthRegImageList, DepthRegImage>(uri, DepthRegImage);

            // Get Object Expecting it Not to Exist
            await GetAndAssert(handler, uri, Energistics.Etp.EtpErrorCodes.NotFound);

            // Put Object for Add
            await PutAndAssert(handler, dataObject);

            // Get Added Object
            var args =await GetAndAssert(handler, uri);

            // Check Added Data Object XML
            Assert.IsNotNull(args?.Message.DataObject);
            var xml = args.Message.DataObject.GetString();

            var result = Parse<DepthRegImageList, DepthRegImage>(xml);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.CommonData.Comments);

            // Remove Comment from Data Object
            result.CommonData.Comments = null;

            var updateDataObject = CreateDataObject<DepthRegImageList, DepthRegImage>(uri, result);

            // Put Object for Update
            await PutAndAssert(handler, updateDataObject);

            // Get Updated Object
            args = await GetAndAssert(handler, uri);

            // Check Added Data Object XML
            Assert.IsNotNull(args?.Message.DataObject);
            var updateXml = args.Message.DataObject.GetString();

            result = Parse<DepthRegImageList, DepthRegImage>(updateXml);
            Assert.IsNotNull(result);

            // Test Data Object overwrite
            Assert.IsNull(result.CommonData.Comments);
        }

        [TestMethod]
        public async Task DepthRegImage141_DeleteObject_Can_Delete_DepthRegImage()
        {
            AddParents();
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = DepthRegImage.GetUri();

            var dataObject = CreateDataObject<DepthRegImageList, DepthRegImage>(uri, DepthRegImage);

            // Get Object Expecting it Not to Exist
            await GetAndAssert(handler, uri, Energistics.Etp.EtpErrorCodes.NotFound);

            // Put Object
            await PutAndAssert(handler, dataObject);

            // Get Object
            var args = await GetAndAssert(handler, uri);

            // Check Data Object XML
            Assert.IsNotNull(args?.Message.DataObject);
            var xml = args.Message.DataObject.GetString();

            var result = Parse<DepthRegImageList, DepthRegImage>(xml);
            Assert.IsNotNull(result);

            // Delete Object
            await DeleteAndAssert(handler, uri);

            // Get Object Expecting it Not to Exist
            await GetAndAssert(handler, uri, Energistics.Etp.EtpErrorCodes.NotFound);
        }
    }
}