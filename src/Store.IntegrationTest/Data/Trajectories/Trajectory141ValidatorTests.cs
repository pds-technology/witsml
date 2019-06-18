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
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    public partial class Trajectory141ValidatorTests
    {

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_406_Missing_Parent_Uid()
        {
            AddParents();

            Trajectory.UidWellbore = null;
            DevKit.AddAndAssert(Trajectory, ErrorCodes.MissingElementUidForAdd);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_406_Missing_Station_Uid()
        {
            AddParents();

            // Add trajectory without stations         
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(1, 0);
            var station = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station);
            station.Uid = null;

            DevKit.AddAndAssert(Trajectory, ErrorCodes.MissingElementUidForAdd);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_407_Missing_Witsml_Object_Type()
        {
            AddParents();

            var response = DevKit.Add<TrajectoryList, Trajectory>(Trajectory, string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_408_Missing_Input_Template()
        {
            AddParents();

            var response = DevKit.AddToStore(ObjectTypes.Trajectory, null, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_409_Non_Conforming_Input_Template()
        {
            AddParents();

            Trajectory.Name = null;
            DevKit.AddAndAssert(Trajectory, ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_440_OptionsIn_Keyword_Not_Recognized()
        {
            AddParents();

            var setting = WitsmlSettings.ThrowForUnsupportedOptionsIn;
            WitsmlSettings.ThrowForUnsupportedOptionsIn = true;

            var response = DevKit.Add<TrajectoryList, Trajectory>(Trajectory, optionsIn: "returnElements=all");
            WitsmlSettings.ThrowForUnsupportedOptionsIn = setting;

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByFunction, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_441_optionsIn_value_not_recognized()
        {
            AddParents();

            var response = DevKit.Add<TrajectoryList, Trajectory>(Trajectory, optionsIn: "compressionMethod=7zip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidKeywordValue, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_442_OptionsIn_Keyword_Not_Supported()
        {
            WitsmlSettings.IsRequestCompressionEnabled = false;

            AddParents();

            var response = DevKit.Add<TrajectoryList, Trajectory>(Trajectory, optionsIn: "compressionMethod=gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByServer, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_443_Uom_Not_Valid()
        {
            AddParents();

            var xmlIn = "<trajectorys xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<trajectory uid=\"" + Trajectory.Uid + "\" uidWell=\"" + Trajectory.UidWell + "\" uidWellbore=\"" + Trajectory.UidWellbore + "\">" + Environment.NewLine +
                    "<nameWell>" + Trajectory.NameWell + "</nameWell>" + Environment.NewLine +
                    "<nameWellbore>" + Trajectory.NameWellbore + "</nameWellbore>" + Environment.NewLine +
                    "<name>" + Trajectory.Name + "</name>" + Environment.NewLine +
                    "<trajectoryStation uid=\"ts01\">" + Environment.NewLine +
                        "<typeTrajStation>unknown</typeTrajStation>" + Environment.NewLine +
                        "<md uom=\"dega\">5673.5</md>" + Environment.NewLine +
                        "<tvd uom=\"ft\">5432.8</tvd>" + Environment.NewLine +
                        "<incl uom=\"dega\">12.4</incl>" + Environment.NewLine +
                        "<azi uom=\"dega\">47.3</azi>" + Environment.NewLine +
                    "</trajectoryStation>" + Environment.NewLine +
                "</trajectory>" + Environment.NewLine +
                "</trajectorys>";

            var response = DevKit.AddToStore(ObjectTypes.Trajectory, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidUnitOfMeasure, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_453_Uom_Not_Specified()
        {
            AddParents();

            var xmlIn = "<trajectorys xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<trajectory uid=\"" + Trajectory.Uid + "\" uidWell=\"" + Trajectory.UidWell + "\" uidWellbore=\"" + Trajectory.UidWellbore + "\">" + Environment.NewLine +
                    "<nameWell>" + Trajectory.NameWell + "</nameWell>" + Environment.NewLine +
                    "<nameWellbore>" + Trajectory.NameWellbore + "</nameWellbore>" + Environment.NewLine +
                    "<name>" + Trajectory.Name + "</name>" + Environment.NewLine +
                    "<trajectoryStation uid=\"ts01\">" + Environment.NewLine +
                        "<typeTrajStation>unknown</typeTrajStation>" + Environment.NewLine +
                        "<md uom=\"\">5673.5</md>" + Environment.NewLine +
                        "<tvd uom=\"ft\">5432.8</tvd>" + Environment.NewLine +
                        "<incl uom=\"dega\">12.4</incl>" + Environment.NewLine +
                        "<azi uom=\"dega\">47.3</azi>" + Environment.NewLine +
                    "</trajectoryStation>" + Environment.NewLine +
                "</trajectory>" + Environment.NewLine +
                "</trajectorys>";

            var response = DevKit.AddToStore(ObjectTypes.Trajectory, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_456_Max_Data_Exceeded_For_Nodes()
        {
            var maxDataNodes = 5;
            WitsmlSettings.TrajectoryMaxDataNodesAdd = maxDataNodes;

            AddParents();

            // Add trajectory without stations         
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(6, 0);
            DevKit.AddAndAssert(Trajectory, ErrorCodes.MaxDataExceeded);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_464_Child_Uids_Not_Unique()
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(2, 0);
            foreach (var station in Trajectory.TrajectoryStation)
            {
                station.Uid = "ts00";
            }

            DevKit.AddAndAssert(Trajectory, ErrorCodes.ChildUidNotUnique);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_478_Parent_Uid_Case_Not_Matching()
        {
            // Base uid
            var uid = Well.Uid;

            // Well Uid with uppercase "P"
            Well.Uid = "P" + uid;
            Wellbore.UidWell = Well.Uid;

            AddParents();

            Trajectory.UidWell = "p" + uid;
            DevKit.AddAndAssert(Trajectory, ErrorCodes.IncorrectCaseParentUid);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_481_Parent_Missing()
        {
            DevKit.AddAndAssert(Trajectory, ErrorCodes.MissingParentDataObject);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_486_Data_Object_Types_Dont_Match()
        {
            AddParents();

            var trajectories = new TrajectoryList { Trajectory = DevKit.List(Trajectory) };
            var xmlIn = EnergisticsConverter.ObjectToXml(trajectories);
            var response = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_GetFromStore_Error_475_Missing_Subset_When_Getting_Growing_Object()
        {
            AddParents();

            DevKit.AddAndAssert(Trajectory);

            var trajectory2 = CreateTrajectory("Tra-2", "Tra-2", Trajectory);

            DevKit.AddAndAssert(trajectory2);

            var query = CreateTrajectory(null, null, Trajectory);

            var result = DevKit.Get<TrajectoryList, Trajectory>(DevKit.List(query), ObjectTypes.Trajectory, null, OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short)ErrorCodes.MissingSubsetOfGrowingDataObject, result.Result);
        }

        private static Trajectory CreateTrajectory(string uid, string name, IWellboreObject trajectoryWellbore)
        {
            return new Trajectory { Uid = uid, Name = name, UidWell = trajectoryWellbore.UidWell, UidWellbore = trajectoryWellbore.UidWellbore, NameWell = trajectoryWellbore.NameWell, NameWellbore = trajectoryWellbore.NameWellbore };
        }
    }
}
