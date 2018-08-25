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
using System.Text;
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes.Object;
using Energistics.Etp.v11.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Compatibility;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;

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
            Assert.IsNotNull(result.MDMin);
            Assert.IsNotNull(result.MDMax);
            Assert.AreEqual(0, result.MDMin.Value);
            Assert.AreEqual(numStations - 1, result.MDMax.Value);
        }

        [TestMethod]
        public async Task Trajectory200_PutGrowingPart_Can_Add_TrajectoryStation()
        {
            AddParents();
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = Trajectory.GetUri();

            var dataObject = CreateDataObject(uri, Trajectory);

            // Put a Trajectory with no stations in the store.
            await PutAndAssert(handler, dataObject);

            // Create a TrajectoryStation and Encode it
            var uid = "TrajStation-1";
            var trajectoryStation = CreateTrajectoryStation(uid, LengthUom.ft, "TestDatum", 1);
            var data = Encoding.UTF8.GetBytes(WitsmlParser.ToXml(trajectoryStation));
            var contentType = EtpContentTypes.Witsml200.For(ObjectTypes.TrajectoryStation);

            // Call PutGrowingPart to add the TrajectoryStation to the Trajectory
            var dataAdapter = DevKit.Container.Resolve<IGrowingObjectDataAdapter>(ObjectNames.Trajectory200);
            dataAdapter.PutGrowingPart(_client.Adapter, uri, contentType, data);

            // Get the Trajectory Object from the store
            var args = await GetAndAssert(handler, uri);

            // Check Data Object XML
            Assert.IsNotNull(args?.Message.DataObject);
            var xml = args.Message.DataObject.GetString();

            var result = Parse<Trajectory>(xml);

            // Validate that the Trajectory could be retrieved from the store and the MDMin matches the station that was entered.
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.MDMin);
            Assert.AreEqual(1, result.MDMin.Value);

            var dataObjectGet = dataAdapter.GetGrowingPart(_client.Adapter, uri, uid);

            var trajectoryStationObject = GetTrajectoryStation(dataObjectGet);

            Assert.IsNotNull(trajectoryStationObject);
            Assert.IsNotNull(trajectoryStationObject.MD);
            Assert.IsNotNull(trajectoryStationObject.Incl);
            Assert.IsNotNull(trajectoryStationObject.Azi);
            Assert.AreEqual(trajectoryStation.MD.Value, trajectoryStationObject.MD.Value);
            Assert.AreEqual(trajectoryStation.Incl.Value, trajectoryStationObject.Incl.Value);
            Assert.AreEqual(trajectoryStation.Azi.Value, trajectoryStationObject.Azi.Value);
        }

        [TestMethod]
        public async Task Trajectory200_GetGrowingParts_Can_Retrieve_Trajectory_Station_Range()
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

            // Get GrowingParts (range)
            const int rangeStart = 10;
            const int rangeEnd = 109;
            var dataAdapter = DevKit.Container.Resolve<IGrowingObjectDataAdapter>(ObjectNames.Trajectory200);
            var growingParts = dataAdapter.GetGrowingParts(_client.Adapter, uri, rangeStart, rangeEnd);


            // Assert that we retrieved the total number of stations expected
            Assert.IsNotNull(growingParts);
            Assert.AreEqual(rangeEnd - rangeStart + 1, growingParts.Count);

            var firstStation = GetTrajectoryStation(growingParts.First());
            var lastStation = GetTrajectoryStation(growingParts.Last());

            // Assert that it's the correct range
            Assert.AreEqual(rangeStart, firstStation.MD.Value);
            Assert.AreEqual(rangeEnd, lastStation.MD.Value);
        }

        [TestMethod]
        public async Task Trajectory200_GetGrowingPart_Can_Execute_Against_Trajectory_Without_Stations()
        {
            AddParents();

            // Allow for Log data to be saved during a Put
            CompatibilitySettings.TrajectoryAllowPutObjectWithData = true;

            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = Trajectory.GetUri();

            // Trajectory without stations
            var dataObject = CreateDataObject(uri, Trajectory);

            // Put Object for Add
            await PutAndAssert(handler, dataObject);

            // Resolve the data adapter for Trajectory200
            var dataAdapter = DevKit.Container.Resolve<IGrowingObjectDataAdapter>(ObjectNames.Trajectory200);

            // Assert that GetGrowingPart does not fail without stations to return
            var dataObjectStation = dataAdapter.GetGrowingPart(_client.Adapter, uri, string.Empty);
            Assert.IsNotNull(dataObjectStation);
            var trajectoryStationObject = GetTrajectoryStation(dataObjectStation);
            Assert.IsNull(trajectoryStationObject);

            // Assert that GetGrowingParts does not fail without stations to return
            var dataObjectStations = dataAdapter.GetGrowingParts(_client.Adapter, uri, 0, 100);
            Assert.IsNotNull(dataObjectStations);
            Assert.AreEqual(0, dataObjectStations.Count);
        }

        private static TrajectoryStation GetTrajectoryStation(IDataObject dataObjectGet)
        {
            if (dataObjectGet.Data == null)
                return null;

            var growingPartXml = dataObjectGet.GetString();
            var trajectoryStationObject = WitsmlParser.Parse<TrajectoryStation>(WitsmlParser.Parse(growingPartXml).Root);
            return trajectoryStationObject;
        }

        private List<TrajectoryStation> CreateTrajectoryStations(int numStations, LengthUom uom, string datum)
        {
            var trajectoryStations = new List<TrajectoryStation>();

            for (var i = 0; i < numStations; i++)
            {
                trajectoryStations.Add(CreateTrajectoryStation($"TrajStation-{i}", uom, datum, i));
            }

            return trajectoryStations;
        }

        private TrajectoryStation CreateTrajectoryStation(string uid, LengthUom uom, string datum, double value)
        {
            return new TrajectoryStation()
            {
                Uid = uid,
                MD = new MeasuredDepthCoord() { Datum = datum, Uom = uom, Value = value },
                Incl = new PlaneAngleMeasure() { Uom = PlaneAngleUom.rad, Value = 0.005 },
                Azi = new PlaneAngleMeasure() { Uom = PlaneAngleUom.rad, Value = 0.002 },
                TypeTrajStation = TrajStationType.MDINCLandAZI
            };
        }
    }
}
