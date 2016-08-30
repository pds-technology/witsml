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
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Trajectories
{
    [TestClass]
    public class Trajectory141ValidatorTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;
        private Trajectory _trajectory;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };

            _wellbore = new Wellbore
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };

            _trajectory = _devKit.CreateTrajectory(_devKit.Uid(), _devKit.Name("Log 01"), _well.Uid, _well.Name, _wellbore.Uid, _wellbore.Name);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WitsmlSettings.MaxStationCount = DevKitAspect.DefaultMaxStationCount;
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_401_No_Plural_Root_Element()
        {
            AddParents();

            var template = "<trajectory xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <trajectory uid=\"{0}\" uidWell=\"{1}\" uidWellbore=\"{2}\">" + Environment.NewLine +
                           "   <nameWell>{3}</nameWell>" + Environment.NewLine +
                           "   <nameWellbore>{4}</nameWellbore>" + Environment.NewLine +
                           "   <name>{5}</name>" + Environment.NewLine +
                           "   </trajectory>" + Environment.NewLine +
                           "</trajectory>";

            var xmlIn = string.Format(template, _trajectory.Uid, _trajectory.UidWell, _trajectory.UidWellbore,
                _trajectory.NameWell, _trajectory.NameWellbore, _trajectory.Name);

            var response = _devKit.AddToStore(ObjectTypes.Trajectory, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_405_Trajectory_Already_Exists()
        {
            AddParents();

            _devKit.AddAndAssert(_trajectory);

            _devKit.AddAndAssert(_trajectory, ErrorCodes.DataObjectUidAlreadyExists);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_406_Missing_Parent_Uid()
        {
            AddParents();

            _trajectory.UidWellbore = null;
            _devKit.AddAndAssert(_trajectory, ErrorCodes.MissingElementUidForAdd);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_406_Missing_Station_Uid()
        {
            AddParents();

            // Add trajectory without stations         
            _trajectory.TrajectoryStation = _devKit.TrajectoryStations(1, 0);
            var station = _trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station);
            station.Uid = null;

            _devKit.AddAndAssert(_trajectory, ErrorCodes.MissingElementUidForAdd);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_407_Missing_Witsml_Object_Type()
        {
            AddParents();

            var response = _devKit.Add<TrajectoryList, Trajectory>(_trajectory, string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_408_Missing_Input_Template()
        {
            AddParents();

            var response = _devKit.AddToStore(ObjectTypes.Trajectory, null, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_409_Non_Conforming_Input_Template()
        {
            AddParents();

            _trajectory.Name = null;
            _devKit.AddAndAssert(_trajectory, ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_442_OptionsIn_Keyword_Not_Supported()
        {
            AddParents();

            var response = _devKit.Add<TrajectoryList, Trajectory>(_trajectory, optionsIn: "compressionMethod=gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByServer, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_453_Uom_Not_Specified()
        {
            AddParents();

            var xmlIn = "<trajectorys xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<trajectory uid=\"" + _trajectory.Uid + "\" uidWell=\"" + _trajectory.UidWell + "\" uidWellbore=\"" + _trajectory.UidWellbore + "\">" + Environment.NewLine +
                    "<nameWell>" + _trajectory.NameWell + "</nameWell>" + Environment.NewLine +
                    "<nameWellbore>" + _trajectory.NameWellbore + "</nameWellbore>" + Environment.NewLine +
                    "<name>" + _trajectory.Name + "</name>" + Environment.NewLine +
                    "<trajectoryStation uid=\"ts01\">" + Environment.NewLine +
                        "<typeTrajStation>unknown</typeTrajStation>" + Environment.NewLine +
                        "<md uom=\"\">5673.5</md>" + Environment.NewLine +
                        "<tvd uom=\"ft\">5432.8</tvd>" + Environment.NewLine +
                        "<incl uom=\"dega\">12.4</incl>" + Environment.NewLine +
                        "<azi uom=\"dega\">47.3</azi>" + Environment.NewLine +
                    "</trajectoryStation>" + Environment.NewLine +
                "</trajectory>" + Environment.NewLine +
                "</trajectorys>";

            var response = _devKit.AddToStore(ObjectTypes.Trajectory, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_456_Max_Data_Exceeded_For_Nodes()
        {
            var maxDataNodes = 5;
            WitsmlSettings.MaxDataNodes = maxDataNodes;

            AddParents();

            // Add trajectory without stations         
            _trajectory.TrajectoryStation = _devKit.TrajectoryStations(6, 0);
            _devKit.AddAndAssert(_trajectory, ErrorCodes.MaxDataExceeded);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_464_Child_Uids_Not_Unique()
        {
            AddParents();

            _trajectory.TrajectoryStation = _devKit.TrajectoryStations(2, 0);
            foreach (var station in _trajectory.TrajectoryStation)
            {
                station.Uid = "ts00";
            }

            _devKit.AddAndAssert(_trajectory, ErrorCodes.ChildUidNotUnique);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_478_Parent_Uid_Case_Not_Matching()
        {
            // Base uid
            var uid = _well.Uid;

            // Well Uid with uppercase "P"
            _well.Uid = "P" + uid;
            _wellbore.UidWell = _well.Uid;

            AddParents();

            _trajectory.UidWell = "p" + uid;
            _devKit.AddAndAssert(_trajectory, ErrorCodes.IncorrectCaseParentUid);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_486_Data_Object_Types_Dont_Match()
        {
            AddParents();

            var trajectories = new TrajectoryList { Trajectory = _devKit.List(_trajectory) };
            var xmlIn = EnergisticsConverter.ObjectToXml(trajectories);
            var response = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        private void AddParents()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
