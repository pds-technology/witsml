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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using Shouldly;

namespace PDS.Witsml.Server
{
    [TestClass]
    public class ContainerTests
    {
        private IContainer _container;

        [TestInitialize]
        public void OnTestSetup()
        {
            _container = ContainerFactory.Create();
            _container.Register(this);
            _container.BuildUp(this);
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            _container.Dispose();
        }

        [TestMethod()]
        public void Container_Resolve_Unknown_Contract_Throws_Error()
        {
            Should.Throw<ContainerException>(() =>
            {
                _container.Resolve<IWitsmlStore>("InvalidContract");
            });
        }

        [TestMethod()]
        public void Container_Resolve_Using_Type()
        {
            var store = _container.Resolve(typeof(IWitsmlStore));

            Assert.IsNotNull(store);
            Assert.AreEqual(typeof(WitsmlStore), store.GetType());
        }

        [TestMethod()]
        public void Container_Resolve_All_Returns_One_IWitsmlStore()
        {
            var store = _container.ResolveAll<IWitsmlStore>();

            Assert.AreEqual(1, store.Count());
        }

        [TestMethod()]
        public void Container_Resolve_All_Using_Type_Returns_One_IWitsmlStore()
        {
            var store = _container.ResolveAll(null, "");

            Assert.IsTrue(!store.Any());

            store = _container.ResolveAll(typeof(IWitsmlStore), "");

            Assert.AreEqual(1, store.Count());
        }
    }
}