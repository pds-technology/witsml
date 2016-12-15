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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Provides validation for <see cref="Trajectory" /> data objects.
    /// </summary>
    public partial class Trajectory131Validator
    {
        /// <summary>
        /// Configures the context.
        /// </summary>
        protected override void ConfigureContext()
        {
            Context.Ignored = new List<string>
            {
                "mdMn", "mdMx"
            };

            if (Context.Function != Functions.AddToStore && Context.Function != Functions.UpdateInStore)
                Context.Ignored.Add("trajectoryStation");
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            var stations = DataObject.TrajectoryStation;

            // Validate common attributes
            foreach (var result in base.ValidateForInsert())
                yield return result;

            if (stations != null)
            {
                foreach (var validationResult in ValidateTrajectoryStations(stations))
                    yield return validationResult;
            }
        }

        /// <summary>
        /// Validates the data object while executing UpdateInStore.
        /// </summary>
        /// <returns>
        /// A collection of validation results.
        /// </returns>
        protected override IEnumerable<ValidationResult> ValidateForUpdate()
        {
            // Validate Trajectory uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) || string.IsNullOrWhiteSpace(DataObject.UidWellbore) || string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
            }
            else
            {
                var uri = DataObject.GetUri();
                var stations = DataObject.TrajectoryStation;
                if (stations != null)
                {
                    foreach (var validationResult in ValidateTrajectoryStations(stations))
                        yield return validationResult;
                }

                var current = DataAdapter.Get(uri);

                // Validate Trajectory does not exist
                if (current == null)
                {
                    yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
                }
            }
        }

        /// <summary>
        /// Validates the data object while executing DeleteFromStore.
        /// </summary>
        /// <returns>
        /// A collection of validation results.
        /// </returns>
        protected override IEnumerable<ValidationResult> ValidateForDelete()
        {
            // Validate Trajectory uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) || string.IsNullOrWhiteSpace(DataObject.UidWellbore) || string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
            }
            else
            {
                var uri = DataObject.GetUri();
                var stations = DataObject.TrajectoryStation;
                if (stations != null)
                {
                    foreach (var validationResult in ValidateTrajectoryStations(stations))
                        yield return validationResult;
                }

                var current = DataAdapter.Get(uri);

                // Validate Trajectory does not exist
                if (current == null)
                {
                    yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
                }
            }
        }

        /// <summary>
        /// Validates the trajectory stations.
        /// </summary>
        /// <param name="stations">The trajectory stations.</param>
        /// <returns>
        /// A collection of validation results.
        /// </returns>
        private IEnumerable<ValidationResult> ValidateTrajectoryStations(List<TrajectoryStation> stations)
        {
            // Only ignore if the UID is present without a value
            if (stations.Any(s => s.Uid != null && string.IsNullOrWhiteSpace(s.Uid)))
            {
                yield return
                    new ValidationResult(ErrorCodes.MissingElementUidForUpdate.ToString(), new[] { "TrajectoryStation", "Uid" });
            }
            else if (stations.HasDuplicateUids())
            {
                yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "TrajectoryStation", "Uid" });
            }
            else if (Context.Function.IsDataNodesValid(ObjectTypes.GetObjectType(DataObject), stations.Count))
            {
                yield return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] { "TrajectoryStation" });
            }
        }
    }
}
