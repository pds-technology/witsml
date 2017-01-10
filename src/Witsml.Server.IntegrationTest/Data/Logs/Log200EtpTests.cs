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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log200EtpTests
    /// </summary>
    public partial class Log200EtpTests
    {
        [TestMethod]
        public async Task Log200_GetResources_Can_Get_Log_Folder_Resources()
        {
            AddParents();
            DevKit.AddAndAssert(Log);

            await RequestSessionAndAssert();

            var uri = Log.GetUri();
            var folderUri = uri.Parent.Append(ObjectFolders.Logs);
            await GetResourcesAndAssert(folderUri);

            var timeLogUri = folderUri.Append(ObjectFolders.Time);
            await GetResourcesAndAssert(timeLogUri);

            var depthLogUri = folderUri.Append(ObjectFolders.Depth);
            await GetResourcesAndAssert(depthLogUri);
        }
    }
}