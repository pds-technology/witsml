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

using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework.Web;
using PDS.WITSMLstudio.Store.Data;

namespace PDS.WITSMLstudio.Store
{
    /// <summary>
    /// Defines properties and methods that can be used to provide
    /// configuration settings for integration tests.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.ITestContextProvider" />
    public class TestContextProvider : ITestContextProvider
    {
        /// <summary>
        /// Configures the specified dev kit.
        /// </summary>
        /// <param name="devKit">The dev kit.</param>
        /// <param name="context">The test context.</param>
        public void Configure(DevKitAspect devKit, TestContext context)
        {
            // Configure DependencyResolver
            GlobalConfiguration.Configuration.DependencyResolver = new DependencyResolver(devKit.Container);

            if (context == null) return;

            if (context.Properties.Contains("WitsmlStoreUrl"))
                devKit.ConnectionUrl = context.Properties["WitsmlStoreUrl"].ToString();

            if (context.Properties.Contains("MongoDbConnection"))
            {
                var connection = context.Properties["MongoDbConnection"].ToString();
                var provider = new DatabaseProvider(new [] { new MongoDbClassMapper() }, connection);
                devKit.Container.Register<IDatabaseProvider>(provider);
            }
        }
    }
}
