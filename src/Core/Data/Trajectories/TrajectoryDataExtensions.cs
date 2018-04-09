using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.WITSMLstudio.Data.Trajectories
{
    /// <summary>
    /// Provides extension methods to work with trajectory data.
    /// </summary>
    public static class TrajectoryDataExtensions
    {
        /// <summary>
        /// Gets a TrajectoryDataReader for an <see cref="Witsml141.Trajectory"/>.
        /// </summary>
        /// <param name="trajectory">The <see cref="Witsml141.Trajectory"/> instance.</param>
        /// <returns>A <see cref="TrajectoryDataReader"/></returns>
        public static TrajectoryDataReader GetReader(this Witsml141.Trajectory trajectory)
        {
            return new TrajectoryDataReader(trajectory);
        }

        /// <summary>
        /// Gets a TrajectoryDataReader for an <see cref="Witsml131.Trajectory"/>.
        /// </summary>
        /// <param name="trajectory">The <see cref="Witsml131.Trajectory"/> instance.</param>
        /// <returns>A <see cref="TrajectoryDataReader"/></returns>
        public static TrajectoryDataReader GetReader(this Witsml131.Trajectory trajectory)
        {
            return new TrajectoryDataReader(trajectory);
        }
    }
}
