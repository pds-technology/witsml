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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.Etp.v11.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.ChannelSets
{
    /// <summary>
    /// ChannelSet200EtpTests
    /// </summary>
    public partial class ChannelSet200EtpTests
    {
        [TestMethod]
        public async Task ChannelSet200_PutObject_Can_Add_ChannelSet_With_Metadata()
        {
            AddParents();
            await RequestSessionAndAssert();

            var holeSizes = new[] { 17.5, 12.25, 8.5, 6.0 };
            var timeDepths = new[] { "Depth", "Time" };
            var runs = new List<Tuple<double, int, double, double>>
            {
                new Tuple<double, int, double, double>(17.5, 1, 0.0, 200.0),
                new Tuple<double, int, double, double>(17.5, 2, 200.0, 435.0),
                new Tuple<double, int, double, double>(12.25, 3, 435.0, 800.0),
                new Tuple<double, int, double, double>(12.25, 4, 800.0, 1250.0),
                new Tuple<double, int, double, double>(12.25, 5, 1250.0, 1400.0),
                new Tuple<double, int, double, double>(8.5, 6, 1400.0, 1675.0),
                new Tuple<double, int, double, double>(8.5, 7, 1675.0, 1900.0),
                new Tuple<double, int, double, double>(8.5, 8, 1900.0, 2305.0),
                new Tuple<double, int, double, double>(8.5, 9, 2305.0, 2700.0),
                new Tuple<double, int, double, double>(6.0, 10, 2700.0, 3100.0),
                new Tuple<double, int, double, double>(6.0, 11, 3100.0, 3450.0),
            };

            foreach (var holeSize in holeSizes)
            {
                foreach (var run in runs.Where(x => x.Item1.Equals(holeSize)))
                {
                    foreach (var timeDepth in timeDepths)
                    {
                        var runNumber = run.Item2.ToString();
                        await CreateChannelSet($"{holeSize}in_drilling_run{runNumber.PadLeft(2, '0')} - {timeDepth} Log", holeSize, timeDepth, run);
                    }
                }
            }
        }

        private async Task CreateChannelSet(string name, double holeSize, string timeDepth, Tuple<double, int, double, double> tuple)
        {
            ChannelSet.Uuid = DevKit.Uid();
            ChannelSet.Citation.Title = name;
            ChannelSet.RunNumber = tuple.Item2.ToString();
            ChannelSet.StartIndex = new DepthIndexValue { Depth = (float)tuple.Item3 };
            ChannelSet.EndIndex = new DepthIndexValue { Depth = (float)tuple.Item4 };
            ChannelSet.NominalHoleSize = new LengthMeasureExt(holeSize, "in");
            ChannelSet.TimeDepth = timeDepth;

            var handler = _client.Handler<IStoreCustomer>();
            var uri = ChannelSet.GetUri();

            var dataObject = CreateDataObject(uri, ChannelSet);

            // Get Object Expecting it Not to Exist
            //await GetAndAssert(handler, uri, Energistics.EtpErrorCodes.NotFound);

            // Put Object
            await PutAndAssert(handler, dataObject);

            // Get Object
            //var args = await GetAndAssert(handler, uri);

            // Check Data Object XML
            //Assert.IsNotNull(args?.Message.DataObject);
            //var xml = args.Message.DataObject.GetString();

            //var result = Parse<ChannelSet>(xml);
            //Assert.IsNotNull(result);
        }
    }
}
