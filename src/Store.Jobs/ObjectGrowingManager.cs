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
using System.ComponentModel.Composition;
using log4net;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;

namespace PDS.WITSMLstudio.Store.Jobs
{
    /// <summary>
    /// Job to manage the updating of the object growing flag in growing objects.
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ObjectGrowingManager
    {
        public static readonly string JobId = typeof(ObjectGrowingManager).FullName;
        private static readonly ILog _log = LogManager.GetLogger(typeof(ObjectGrowingManager));
        private static readonly object _lock = new object();
        private static bool _isExpiringGrowingObjects;

        private readonly IGrowingObjectDataProvider _growingObjectDataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectGrowingManager" /> class.
        /// </summary>
        /// <param name="growingObjectDataProvider">The growing object data provider.</param>
        [ImportingConstructor]
        public ObjectGrowingManager(IGrowingObjectDataProvider growingObjectDataProvider)
        {
            _growingObjectDataProvider = growingObjectDataProvider;
        }

        /// <summary>
        /// Starts the process to verify the object growing status of growing objects.
        /// </summary>
        public void Start()
        {
            const string message = "Object Growing Expiration Job is already running";

            if (_isExpiringGrowingObjects)
            {
                _log.Warn(message);
                return;
            }

            lock (_lock)
            {
                if (_isExpiringGrowingObjects)
                {
                    _log.Warn(message);
                    return;
                }

                _log.Debug("Starting Object Growing Expiration Job");
                _isExpiringGrowingObjects = true;
            }

            // TODO: Implement a way to pause/restart the job at runtime.

            try
            {
                ExpireGrowingObjects();
            }
            finally
            {
                _isExpiringGrowingObjects = false;
            }
        }

        /// <summary>
        /// Expires the growing objects for the specified objectType and expiredDateTime
        /// </summary>
        /// <returns></returns>
        internal void ExpireGrowingObjects()
        {
            var wellboreUris = new List<string>();

            var logWellboreUris = _growingObjectDataProvider.ExpireGrowingObjects(ObjectTypes.Log,
                DateTime.UtcNow.AddSeconds(-1 * WitsmlSettings.LogGrowingTimeoutPeriod));
            wellboreUris.AddRange(logWellboreUris);

            var trajectoryWellboreUris = _growingObjectDataProvider.ExpireGrowingObjects(ObjectTypes.Trajectory,
                DateTime.UtcNow.AddSeconds(-1 * WitsmlSettings.TrajectoryGrowingTimeoutPeriod));
            wellboreUris.AddRange(trajectoryWellboreUris);

            var mudLogWellboreUris = _growingObjectDataProvider.ExpireGrowingObjects(ObjectTypes.MudLog,
                DateTime.UtcNow.AddSeconds(-1 * WitsmlSettings.MudLogGrowingTimeoutPeriod));
            wellboreUris.AddRange(mudLogWellboreUris);

            _growingObjectDataProvider.ExpireWellboreObjects(wellboreUris);
        }
    }
}
