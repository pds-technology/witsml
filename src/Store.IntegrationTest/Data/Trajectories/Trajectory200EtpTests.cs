//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Energistics.Common;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Compatibility;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Trajectory200EtpTests
    /// </summary>
    public partial class Trajectory200EtpTests
    {
        [TestMethod]
        public async Task Trajectory200_PutObject_Can_Add_Trajectory_Data_With_TrajectoryAllowPutObjectWithData_True()
        {
            AddParents();

            // Allow for Log data to be saved during a Put
            CompatibilitySettings.TrajectoryAllowPutObjectWithData = true;

            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = Trajectory.GetUri();

            // Add Trajectory Stations
            const int numStations = 150;
            Trajectory.TrajectoryStation =
                CreateTrajectoryStations(numStations, uom: LengthUom.ft, datum: "Test Datum");

            var dataObject = CreateDataObject(uri, Trajectory);

            // Put Object for Add
            await PutAndAssert(handler, dataObject);

            // Get Added Object
            var args = await GetAndAssert(handler, uri);

            // Check Added Data Object XML
            Assert.IsNotNull(args?.Message.DataObject);
            var xml = args.Message.DataObject.GetString();

            var result = Parse<Trajectory>(xml);

            Assert.IsNotNull(result);
            // TODO: Add back in when 2.0 Put is fixed to update MDMin/MDMax
            //Assert.AreEqual(0, result.MDMin);
            //Assert.AreEqual(numStations - 1, result.MDMin);
        }

        private List<TrajectoryStation> CreateTrajectoryStations(int numStations, LengthUom uom, string datum)
        {
            var trajectoryStations = new List<TrajectoryStation>();

            for (var i = 0; i < numStations; i++)
            {
                trajectoryStations.Add(new TrajectoryStation()
                {
                    Uid = $"TrajStation-{i}",
                    MD = new MeasuredDepthCoord() { Datum = datum, Uom = uom, Value = i },
                    Incl = new PlaneAngleMeasure() { Uom = PlaneAngleUom.rad, Value = 0.005 },
                    Azi = new PlaneAngleMeasure() { Uom = PlaneAngleUom.rad, Value = 0.002 },
                    TypeTrajStation = new TrajStationType?(TrajStationType.MDINCLandAZI)
                });
            }

            return trajectoryStations;
        }
    }
}
