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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Store.Data;

namespace PDS.WITSMLstudio.Store
{
    public class DevKit200Aspect : DevKitAspect
    {
        public DevKit200Aspect(TestContext context) : base(null, (WMLSVersion)2, context)
        {
            LogGenerator = new Log200Generator();
        }

        public Log200Generator LogGenerator { get; }

        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version200.Value; }
        }

        public Citation Citation(string name)
        {
            return LogGenerator.CreateCitation(name);
        }

        public GeodeticWellLocation Location()
        {
            return new GeodeticWellLocation()
            {
                Crs = new GeodeticEpsgCrs() { EpsgCode = 26914 },
                Latitude = new PlaneAngleMeasure(28.5597, PlaneAngleUom.Item0001seca),
                Longitude = new PlaneAngleMeasure(-90.6671, PlaneAngleUom.Item0001seca),
                Uid = "loc-01"
            };
        }

        public DataObjectReference DataObjectReference(AbstractObject dataObject)
        {
            var objectType = ObjectTypes.GetObjectType(dataObject);
            return DataObjectReference(objectType, dataObject.Citation?.Title, dataObject.Uuid);
        }

        public DataObjectReference DataObjectReference(string objectType, string title = null, string uuid = null)
        {
            return new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(objectType),
                Title = title ?? objectType,
                Uuid = uuid ?? objectType,
            };
        }

        public void InitHeader(Log log, LoggingMethod loggingMethod, ChannelIndex channelIndex, IndexDirection direction = IndexDirection.increasing)
        {
            log.ChannelSet = new List<ChannelSet>();
            log.LoggingMethod = loggingMethod;

            var index = List(channelIndex);
            if (channelIndex.IndexType == ChannelIndexType.measureddepth)
            {
                log.TimeDepth = "depth";

                var pointMetadataList = List(LogGenerator.CreatePointMetadata("Quality", "Quality", EtpDataType.boolean));

                ChannelSet channelSet = LogGenerator.CreateChannelSet(log);
                channelSet.Index = index;

                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "Rate of Penetration", "ROP", UnitOfMeasure.mh, "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "Hookload", "HKLD", UnitOfMeasure.klbf, "Force", EtpDataType.@double, null));
                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "GR1AX", "GR", UnitOfMeasure.gAPI, "Gamma_Ray", EtpDataType.@double, null));

                CreateMockChannelSetData(channelSet, channelSet.Index);
                log.ChannelSet.Add(channelSet);

            }
            else if (channelIndex.IndexType == ChannelIndexType.datetime)
            {
                log.TimeDepth = "time";

                var pointMetadataList = List(LogGenerator.CreatePointMetadata("Confidence", "Confidence", EtpDataType.@float));

                ChannelSet channelSet = LogGenerator.CreateChannelSet(log);
                channelSet.Index = index;

                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "Rate of Penetration", "ROP", UnitOfMeasure.mh, "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "GR1AX", "GR", UnitOfMeasure.gAPI, "Gamma_Ray", EtpDataType.@double, null));

                CreateMockChannelSetData(channelSet, channelSet.Index);
                log.ChannelSet.Add(channelSet);
            }
        }

        public void CreateMockChannelSetData(ChannelSet channelSet, List<ChannelIndex> indices)
        {
            channelSet.Data = new ChannelData()
            {
                FileUri = "file://",
            };

            if (indices.Count == 1)
            {
                if (indices[0].IndexType == ChannelIndexType.measureddepth)
                {
                    channelSet.SetData(@"[
                            [ [0.0 ], [ [ 1.0, true  ], 2.0,  3.0 ] ],
                            [ [0.1 ], [ [ 1.1, false ], null, 3.1 ] ],
                            [ [0.2 ], [ null,           null, 3.2 ] ],
                            [ [0.3 ], [ [ 1.3, true  ], 2.3,  3.3 ] ]
                        ]");
                }
                else if (indices[0].IndexType == ChannelIndexType.datetime)
                {
                    channelSet.SetData(@"[
                            [ [ ""2016-01-01T00:00:00.0000Z"" ], [ [ 1.0, true  ], 2.0,  3.0 ] ],
                            [ [ ""2016-01-01T00:00:01.0000Z"" ], [ [ 1.1, false ], null, 3.1 ] ],
                            [ [ ""2016-01-01T00:00:02.0000Z"" ], [ null,           null, 3.2 ] ],
                            [ [ ""2016-01-01T00:00:03.0000Z"" ], [ [ 1.3, true  ], 2.3,  3.3 ] ]
                        ]");
                }
            }
            else if (indices.Count == 2)
            {
                if (indices[0].IndexType == ChannelIndexType.measureddepth)
                {
                    channelSet.SetData(@"[
                            [ [0.0, ""2016-01-01T00:00:00.0000Z"" ], [ [1.0, true  ],  2.0,  3.0 ] ],
                            [ [0.1, ""2016-01-01T00:00:01.0000Z"" ], [ [1.1, false ],  null, 3.1 ] ],
                            [ [0.2, ""2016-01-01T00:00:02.0000Z"" ], [ null,           null, 3.2 ] ],
                            [ [0.3, ""2016-01-01T00:00:03.0000Z"" ], [ [1.3, true  ],  2.3,  3.3 ] ]
                        ]");
                }
                else if (indices[0].IndexType == ChannelIndexType.datetime)
                {
                    channelSet.SetData(@"[
                            [ [ ""2016-01-01T00:00:00.0000Z"", 0.0 ], [ [ 1.0, true  ], 2.0,  3.0 ] ],
                            [ [ ""2016-01-01T00:00:01.0000Z"", 0.1 ], [ [ 1.1, false ], null, 3.1 ] ],
                            [ [ ""2016-01-01T00:00:02.0000Z"", 0.2 ], [ null,           null, 3.2 ] ],
                            [ [ ""2016-01-01T00:00:03.0000Z"", 0.3 ], [ [ 1.3, true  ], 2.3,  3.3 ] ]
                        ]");
                }
            }
        }

        /// <summary>
        /// Initializes the channel set.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="indexList">The index list.</param>
        /// <param name="loggingMethod">The logging method.</param>
        /// <param name="numDataValue">The number data value.</param>
        public void InitChannelSet(Log log, List<ChannelIndex> indexList, LoggingMethod loggingMethod = LoggingMethod.computed, int numDataValue = 150)
        {
            ChannelSet channelSet = LogGenerator.CreateChannelSet(log);
            channelSet.Index = indexList;
            bool isDepth = log.TimeDepth.EqualsIgnoreCase(ObjectFolders.Depth);

            if (isDepth)
            {
                var pointMetadataList = List(LogGenerator.CreatePointMetadata("Quality", "Quality", EtpDataType.boolean));

                channelSet.Channel.Add(LogGenerator.CreateChannel(log, indexList, "Rate of Penetration", "ROP", UnitOfMeasure.mh, "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
                channelSet.Channel.Add(LogGenerator.CreateChannel(log, indexList, "Hookload", "HKLD", UnitOfMeasure.klbf, "Force", EtpDataType.@double, null));
            }
            else
            {
                var pointMetadataList = List(LogGenerator.CreatePointMetadata("Confidence", "Confidence", EtpDataType.@float));

                channelSet.Channel.Add(LogGenerator.CreateChannel(log, indexList, "Rate of Penetration", "ROP", UnitOfMeasure.mh, "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
            }

            log.ChannelSet = new List<ChannelSet>();
            log.ChannelSet.Add(channelSet);

            LogGenerator.GenerateChannelData(log.ChannelSet, numDataValue: numDataValue);
        }

        /// <summary>
        /// Creates an ExtensionNameValue object.
        /// </summary>
        /// <param name="name">The name to be assigned to the ExtensionNameValue.</param>
        /// <param name="value">The value to be assigned to the ExtensionNameValue.</param>
        /// <returns>An ExtensionNameValue object with the specified name and value</returns>
        public ExtensionNameValue ExtensionNameValue(string name, string value)
        {
            return new ExtensionNameValue()
            {
                Name = name,
                Value = new StringMeasure() { Value = value }
            };
        }

        /// <summary>
        /// Creates the log.
        /// </summary>
        /// <param name="indexType">Type of the index.</param>
        /// <param name="isIncreasing">if set to <c>true</c> [is increasing].</param>
        /// <returns></returns>
        public Log CreateLog(ChannelIndexType indexType, bool isIncreasing)
        {
            Log log = new Log();
            log.Citation = Citation("ChannelSet");
            log.Uuid = Uid();

            log.ChannelSet = new List<ChannelSet>();
            log.Wellbore = DataObjectReference(ObjectTypes.Wellbore, Name("Wellbore"), Uid());

            List<ChannelIndex> indexList = new List<ChannelIndex>();
            IndexDirection direction = isIncreasing ? IndexDirection.increasing : IndexDirection.decreasing;

            if (LogGenerator.DepthIndexTypes.Contains(indexType))
            {
                log.TimeDepth = ObjectFolders.Depth;
                ChannelIndex channelIndex = LogGenerator.CreateMeasuredDepthIndex(direction);

                if (indexType.Equals(ChannelIndexType.trueverticaldepth))
                {
                    channelIndex = LogGenerator.CreateTrueVerticalDepthIndex(direction);
                }
                else if (indexType.Equals(ChannelIndexType.passindexeddepth))
                {
                    channelIndex = LogGenerator.CreatePassIndexDepthIndex(direction);
                }

                indexList.Add(channelIndex);
            }
            else if (LogGenerator.TimeIndexTypes.Contains(indexType))
            {
                log.TimeDepth = ObjectFolders.Time;

                if (indexType.Equals(ChannelIndexType.datetime))
                {
                    // DateTime should be increasing only
                    indexList.Add(LogGenerator.CreateDateTimeIndex());
                }
                else if (indexType.Equals(ChannelIndexType.elapsedtime))
                {
                    indexList.Add(LogGenerator.CreateElapsedTimeIndex(direction));
                }
            }
            else
            {
                log.TimeDepth = ObjectFolders.Other;
                return null;
            }

            InitChannelSet(log, indexList);

            return log;
        }

        public T GetAndAssert<T>(T dataObject, bool isNotNull = true) where T : AbstractObject
        {
            var dataAdapter = Container.Resolve<IWitsmlDataAdapter<T>>();
            var current = dataAdapter.Get(dataObject.GetUri());

            Assert.AreEqual(isNotNull, current != null);
            return current;
        }

        public void AddAndAssert<T>(T dataObject) where T : AbstractObject
        {
            var dataAdapter = Container.Resolve<IWitsmlDataAdapter<T>>();
            dataAdapter.Add(Parser(dataObject), dataObject);

            var exists = dataAdapter.Exists(dataObject.GetUri());
            Assert.IsTrue(exists);
        }

        public void UpdateAndAssert<T>(T dataObject) where T : AbstractObject
        {
            var dataAdapter = Container.Resolve<IWitsmlDataAdapter<T>>();
            dataAdapter.Update(Parser(dataObject), dataObject);

            var exists = dataAdapter.Exists(dataObject.GetUri());
            Assert.IsTrue(exists);
        }

        public void DeleteAndAssert<T>(T dataObject) where T : AbstractObject
        {
            var uri = dataObject.GetUri();
            var dataAdapter = Container.Resolve<IWitsmlDataAdapter<T>>();
            dataAdapter.Delete(uri);

            var exists = dataAdapter.Exists(uri);
            Assert.IsFalse(exists);
        }

        public void EnsureAndAssert<T>(T dataObject) where T : AbstractObject
        {
            var uri = dataObject.GetUri();
            var dataProvider = Container.Resolve<IEtpDataProvider<T>>();
            dataProvider.Ensure(uri);

            var exists = dataProvider.Exists(uri);
            Assert.IsTrue(exists);

            var current = dataProvider.Get(dataObject.GetUri());
            Assert.AreEqual(uri, current.GetUri());
        }
    }
}
