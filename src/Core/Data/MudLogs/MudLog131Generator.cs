//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;

namespace PDS.WITSMLstudio.Data.MudLogs
{
    /// <summary>
    /// Generates data for a 141 MudLog.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataGenerator" />
    public class MudLog131Generator : DataGenerator
    {
        /// <summary>
        /// The default Md uom
        /// </summary>
        public const MeasuredDepthUom MdUom = MeasuredDepthUom.m;
        /// <summary>
        /// The default Tvd uom
        /// </summary>
        public const WellVerticalCoordinateUom TvdUom = WellVerticalCoordinateUom.m;
        private const string GeologyIntervalUidPrefix = "geo-";
        private const string LithologyUidPrefix = "lith-";

        /// <summary>
        /// Generations mudlog geology interval data.
        /// </summary>
        /// <param name="numOfIntervals">The number of geology intervals.</param>
        /// <param name="startMd">The start md.</param>
        /// <param name="mdUom">The MD index uom.</param>
        /// <param name="tvdUom">The TVD index uom.</param>
        /// <param name="includeChromatograph">True if to include chromatograph node.</param>
        /// <param name="includeLithology">True if to generate lithlogy node.</param>
        /// <param name="includeShow">True if to generate show node.</param>
        /// <param name="mixInIntrepreted">True if to generate interpreted geology intervals geology intervals.</param>
        /// <returns>The mudlog geology interval collection.</returns>
        public List<GeologyInterval> GenerateGeologyIntervals(int numOfIntervals, double startMd, MeasuredDepthUom mdUom = MdUom, WellVerticalCoordinateUom tvdUom = TvdUom, bool includeLithology = false, bool includeShow = false, bool includeChromatograph = false, bool mixInIntrepreted = false)
        {
            var geologyIntervals = new List<GeologyInterval>();
            var random = new Random(numOfIntervals * 2);

            for (var i = 1; i < numOfIntervals + 1; i++)
            {
                var startVal = i == 1 ? startMd : startMd + ((i - 1) * 50);
                var mdTop = new MeasuredDepthCoord(startVal, mdUom);
                var mdBottom = new MeasuredDepthCoord(startVal + 50.0, MdUom);
                var uidSuffix = i.ToString();
                var geologyInterval = GenerateGeologyInterval(random, mdTop, mdBottom, uidSuffix);

                if (includeShow)
                {
                    geologyInterval.Show = GenerateShow();
                }

                if (includeChromatograph)
                {
                    geologyInterval.Chromatograph = GenerateChromatograph(mdBottom.Value);
                }

                if (includeLithology)
                {
                    geologyInterval.Lithology = GenerateLithologyList();
                }

                geologyIntervals.Add(geologyInterval);
            }

            return geologyIntervals;
        }

        private List<Lithology> GenerateLithologyList()
        {
            return new List<Lithology>()
            {
                GenerateLithology(1),
                GenerateLithology(2)
            };
        }

        private Lithology GenerateLithology(int uidSuffix)
        {
            return new Lithology()
            {
                Uid = LithologyUidPrefix + uidSuffix,
                Type = uidSuffix == 1 ? LithologyType.Clay : LithologyType.Sandstone,
                Description = uidSuffix == 1 ? "Clay Clay-shale" : "Sandstone: vf-f, clr-frost, mod srt, gd vis por",
            };
        }

        private Chromatograph GenerateChromatograph(double bottom)
        {
            return new Chromatograph()
            {
                DateTime = new Timestamp(DateTime.UtcNow),
                MDTop = new MeasuredDepthCoord(bottom * .99, MdUom),
                MDBottom = new MeasuredDepthCoord(bottom * .995, MdUom),
                WeightMudIn = new DensityMeasure(10.4, DensityUom.lbmft3),
                WeightMudOut = new DensityMeasure(10.3, DensityUom.lbmft3)
            };
        }

        private static Show GenerateShow()
        {
            return new Show()
            {
                ShowRat = ShowRating.fair,
                StainColor = "brown",
                CutColor = "white"
            };
        }

        private static GeologyInterval GenerateGeologyInterval(Random random, MeasuredDepthCoord mdTop, MeasuredDepthCoord mdBottom, string uidSuffix)
        {
            return new GeologyInterval
            {
                Uid = GeologyIntervalUidPrefix + uidSuffix,
                TypeLithology = LithologySource.cuttings,
                MDTop = mdTop,
                MDBottom = mdBottom,
                DateTime = new Timestamp(DateTimeOffset.UtcNow),
                TvdTop = new WellVerticalDepthCoord(mdTop.Value * .805, TvdUom),
                TvdBase = new WellVerticalDepthCoord(mdBottom.Value * .805, TvdUom),
                RopAverage = new VelocityMeasure(random.NextDouble(), VelocityUom.fth),
                RopMin = new VelocityMeasure(random.NextDouble(), VelocityUom.fth),
                RopMax = new VelocityMeasure(random.NextDouble(), VelocityUom.fth),
                WobAverage = new ForceMeasure(random.NextDouble(), ForceUom.klbf),
                TorqueAverage = new MomentOfForceMeasure(random.NextDouble(), MomentOfForceUom.lbfft),
                RpmAverage = new AnglePerTimeMeasure(random.NextDouble(), AnglePerTimeUom.rpm),
                WeightMudAverage = new DensityMeasure(10.5, DensityUom.lbmft3),
                EcdTdAverage = new DensityMeasure(10.6, DensityUom.lbmft3),
                DxcAverage = random.NextDouble()
            };
        }
    }
}
