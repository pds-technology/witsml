//----------------------------------------------------------------------- 
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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server
{
    /// <summary>
    /// Defines properties and methods that can be used to provide
    /// configuration settings for integration tests.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.ITestContextProvider" />
    [Export(typeof(ITestContextProvider))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TestContextProvider : ITestContextProvider
    {
        /// <summary>
        /// Configures the specified dev kit.
        /// </summary>
        /// <param name="devKit">The dev kit.</param>
        /// <param name="context">The test context.</param>
        public void Configure(DevKitAspect devKit, TestContext context)
        {
            if (devKit == null || context == null) return;

            if (context.Properties.Contains("WitsmlStoreUrl"))
                devKit.ConnectionUrl = context.Properties["WitsmlStoreUrl"].ToString();

            if (context.Properties.Contains("MongoDbConnection"))
            {
                var connection = context.Properties["MongoDbConnection"].ToString();
                var provider = new DatabaseProvider(devKit.Container, new MongoDbClassMapper(), connection);
                devKit.Container.Register<IDatabaseProvider>(provider);
            }
        }
    }
}
