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

using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Channels
{
    [TestClass]
    public partial class Channel200ValidatorTests
    {
		public Well Well { get; set; }
		public Wellbore Wellbore { get; set; }
        public Channel Channel { get; set; }
        public DevKit200Aspect DevKit { get; set; }
        public TestContext TestContext { get; set; }
        public List<Channel> QueryEmptyList { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect(TestContext);

            Well = new Well
			{
				Uuid = DevKit.Uid(),
				Citation = DevKit.Citation("Well"),
				GeographicLocationWGS84 = DevKit.Location(),
				TimeZone = DevKit.TimeZone
			};
            Wellbore = new Wellbore
			{
				Uuid = DevKit.Uid(),
				Citation = DevKit.Citation("Wellbore"),
				Well = DevKit.DataObjectReference(Well)
			};
			Channel = new Channel
			{
				Uuid = DevKit.Uid(),
				Citation = DevKit.Citation("Channel"),
				Wellbore = DevKit.DataObjectReference(Wellbore)
			};

            QueryEmptyList = DevKit.List(new Channel());
			OnTestSetUp();
        }

        [TestCleanup]
        public void TestCleanup()
        {
			OnTestCleanUp();
            DevKit = null;
        }

		partial void OnTestSetUp();

		partial void OnTestCleanUp();
	}
}