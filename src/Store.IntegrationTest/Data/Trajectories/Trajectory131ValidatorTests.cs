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
using Energistics.DataAccess.WITSML131;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    public partial class Trajectory131ValidatorTests
    {

        [TestMethod]
        public void Trajectory131Validator_AddToStore_Error_406_Missing_Parent_Uid()
        {
            AddParents();

            Trajectory.UidWellbore = null;
            DevKit.AddAndAssert(Trajectory, ErrorCodes.MissingElementUidForAdd);
        }

        [TestMethod]
        public void Trajectory131Validator_AddToStore_Error_406_Missing_Station_Uid()
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
        public void Trajectory131Validator_AddToStore_Error_407_Missing_Witsml_Object_Type()
        {
            AddParents();

            var response = DevKit.Add<TrajectoryList, Trajectory>(Trajectory, string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod]
        public void Trajectory131Validator_AddToStore_Error_408_Missing_Input_Template()
        {
            AddParents();

            var response = DevKit.AddToStore(ObjectTypes.Trajectory, null, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Trajectory131Validator_AddToStore_Error_409_Non_Conforming_Input_Template()
        {
            AddParents();

            Trajectory.Name = null;
            DevKit.AddAndAssert(Trajectory, ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod]
        public void Trajectory131Validator_AddToStore_Error_443_Uom_Not_Valid()
        {
            AddParents();

            var xmlIn = "<trajectorys xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\">" + Environment.NewLine +
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
        public void Trajectory131Validator_AddToStore_Error_453_Uom_Not_Specified()
        {
            AddParents();

            var xmlIn = "<trajectorys xmlns=\"http://www.witsml.org/schemas/131\" version =\"1.3.1.1\">" + Environment.NewLine +
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
        public void Trajectory131Validator_AddToStore_Error_464_Child_Uids_Not_Unique()
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
        public void Trajectory131Validator_AddToStore_Error_478_Parent_Uid_Case_Not_Matching()
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
        public void Trajectory131Validator_AddToStore_Error_481_Parent_Missing()
        {
            DevKit.AddAndAssert(Trajectory, ErrorCodes.MissingParentDataObject);
        }

        [TestMethod]
        public void Trajectory131Validator_AddToStore_Error_486_Data_Object_Types_Dont_Match()
        {
            AddParents();

            var trajectories = new TrajectoryList { Trajectory = DevKit.List(Trajectory) };
            var xmlIn = EnergisticsConverter.ObjectToXml(trajectories);
            var response = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }
    }
}
