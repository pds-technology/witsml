using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a 141 <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML141.Log}" />
    /// <seealso cref="PDS.Witsml.Server.Configuration.IWitsml141Configuration" />
    [Export(typeof(IWitsml141Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [Export(typeof(IEtpDataAdapter<Log>))]
    [Export141(ObjectTypes.Log, typeof(IEtpDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log141DataAdapter : MongoDbDataAdapter<Log>, IWitsml141Configuration
    {
        private static readonly string DbCollectionNameLogDataValues = "logDataValues";
        private static readonly int LogIndexRangeSize = PDS.Server.MongoDb.Settings.Default.LogIndexRangeSize;
        private static readonly int maxDataNodes = Settings.Default.MaxDataNodes;
        private static readonly int maxDataPoints = Settings.Default.MaxDataPoints;


        /// <summary>
        /// Initializes a new instance of the <see cref="Log141DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Log141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Log141)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Log"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            capServer.Add(Functions.GetFromStore, new ObjectWithConstraint(ObjectTypes.Log)
            {
                MaxDataNodes = maxDataNodes,
                MaxDataPoints = maxDataPoints
            });

            capServer.Add(Functions.AddToStore, new ObjectWithConstraint(ObjectTypes.Log)
            {
                MaxDataNodes = maxDataNodes,
                MaxDataPoints = maxDataPoints
            });

            capServer.Add(Functions.UpdateInStore, ObjectTypes.Log);
            //capServer.Add(Functions.DeleteFromStore, ObjectTypes.Well);
        }

        /// <summary>
        /// Queries the object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>Queried objects.</returns>
        public override WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser)
        {
            var returnElements = parser.ReturnElements();
            Logger.DebugFormat("Querying with return elements '{0}'", returnElements);

            var fields = OptionsIn.ReturnElements.IdOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" }
                : OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements) ? GetLogHeaderFields()
                : OptionsIn.ReturnElements.DataOnly.Equals(returnElements) ? new List<string> { "LogCurveInfo" }
                : null;

            var ignored = new List<string> { "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex", "logData" };
            var logs = QueryEntities(parser, fields, ignored);

            // Only get the LogData returnElements != "header-only" and returnElements != "id-only"
            if (!OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements) && !OptionsIn.ReturnElements.IdOnly.Equals(returnElements))
            {
                logs.ForEach(l =>
                {
                    l.LogData = new List<LogData> { QueryLogDataValues(l, parser) };
                });
            }

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                new LogList()
                {
                    Log = logs
                });
        }

        private List<string> GetLogHeaderFields()
        {
            return new List<string>
            {
                "NameWell",
                "NameWellbore",
                NamePropertyName,
                "ObjectGrowing",
                "DataUpateRate",
                "CurveSensorsAligned",
                "DataGroup",
                "ServiceCompany",
                "RunNumber",
                "BhaRunNumber",
                "Pass",
                "CreationDate",
                "Description",
                "DataDelimiter",
                "IndexType",
                "StartIndex",
                "EndIndex",
                "StepIncrement",
                "StartDateTimeIndex",
                "EndDateTimeIndex",
                "Direction",
                "IndexCurve",
                "NullValue",
                "LogParam",
                "LogCurveInfo",
               // TODO: uncommented the following line when DateTime issue is resolved
               // "CommonData", 
                "CustomData",
                "UidWell",
                "UidWellbore",
                IdPropertyName
            };
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Log> GetAll(EtpUri? parentUri = null)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                var ids = parentUri.Value.GetObjectIds().ToDictionary(x => x.Key, y => y.Value);
                var uidWellbore = ids[ObjectTypes.Wellbore];
                var uidWell = ids[ObjectTypes.Well];

                query = query.Where(x => x.UidWell == uidWell && x.UidWellbore == uidWellbore);
            }

            return query
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Adds a <see cref="Log"/> entity to the data store.
        /// </summary>
        /// <param name="entity">The Log instance to add to the store.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(Log entity)
        {
            entity.Uid = NewUid(entity.Uid);
            entity.CommonData = entity.CommonData.Update();

            var validator = Container.Resolve<IDataObjectValidator<Log>>();
            validator.Validate(Functions.AddToStore, entity);

            try
            {
                // Separate the LogData.Data from the Log
                LogData logData = null;
                if (entity.LogData != null && entity.LogData.Count > 0)
                {
                    logData = entity.LogData.First();
                    entity.LogData.Clear();
                }

                // Save the log and verify
                InsertEntity(entity);

                // If there is any LogData.Data then save it.
                if (logData != null)
                {
                    var indexChannel = new ChannelIndexInfo
                    {
                        Mnemonic = entity.IndexCurve,
                        Increasing = entity.Direction != LogIndexDirection.decreasing,
                        IsTimeIndex = entity.IndexType == LogIndexType.datetime || entity.IndexType == LogIndexType.elapsedtime
                    };
                    var channelDataAdapter = new ChannelDataAdapter(DatabaseProvider);
                    channelDataAdapter.WriteLogDataValues(entity.Uid, logData.Data, logData.MnemonicList, logData.UnitList, indexChannel);
                }

                return new WitsmlResult(ErrorCodes.Success, entity.Uid);
            }
            catch (Exception ex)
            {
                return new WitsmlResult(ErrorCodes.Unset, ex.Message + "\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Computes the range of the data chunk containing the given index value for a given rangeSize.
        /// </summary>
        /// <param name="index">The index value contained within the computed range.</param>
        /// <param name="rangeSize">Size of the range.</param>
        /// <returns>A <see cref="Tuple{int, int}"/> containing the computed range.</returns>
        public Tuple<int, int> ComputeRange(double index, int rangeSize)
        {
            var rangeIndex = (int)(Math.Floor(index / rangeSize));
            return new Tuple<int, int>(rangeIndex * rangeSize, rangeIndex * rangeSize + rangeSize);

        }

        /// <summary>
        /// Updates the specified <see cref="Log"/> instance in the store.
        /// </summary>
        /// <param name="entity">The <see cref="Log"/> instance.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Update(Log entity)
        {
            //List<LogDataValues> logDataValuesList = null;

            // Separate the LogData.Data from the Log
            var logData = ExtractLogDataData(entity);

            if (logData.Any())
            {
                // Start of the first range
                var startIndex = ComputeRange(double.Parse(logData[0].Split(',')[0]), LogIndexRangeSize).Item1;

                // End of the last range
                var endIndex = ComputeRange(double.Parse(logData[logData.Count - 1].Split(',')[0]), LogIndexRangeSize).Item2;

                // Merge with updateLogData sequence
                WriteLogDataValues(entity,
                    ToChunks(
                        MergeSequence(
                            ToSequence(QueryLogDataValues(entity, startIndex, endIndex, false)),
                            GetSequence(string.Empty, logData))));
            }

            // TODO: Fix later
            //UpdateLogHeaderRanges(entity);

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>An instance of <see cref="Log" />.</returns>
        protected override Log Parse(string xml)
        {
            var list = WitsmlParser.Parse<LogList>(xml);
            return list.Log.FirstOrDefault();
        }

        /// <summary>
        /// Gets a list of logs with on the required log header properties.
        /// </summary>
        /// <param name="logs">The logs.</param>
        /// <returns>
        /// A <see cref="IEnumerable{Log}" /> with only the required Log header properties.
        /// </returns>
        private IEnumerable<Log> GetLogHeaderRequiredProperties(List<Log> logs)
        {
            var logsRequired = new List<Log>();

            logs.ForEach(x =>
            {
                logsRequired.Add(new Log()
                {
                    Uid = x.Uid,
                    UidWell = x.UidWell,
                    UidWellbore = x.UidWellbore,
                    Name = x.Name,
                    NameWell = x.NameWell,
                    NameWellbore = x.NameWellbore,
                    IndexType = x.IndexType,
                    IndexCurve = x.IndexCurve
                });
            });

            return logsRequired;
        }

        /// <summary>
        /// Queries the log data values as specified by the parser.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="parser">The parser.</param>
        /// <returns>A <see cref="LogData" /> instance with the LogData.Data specified by the parser.</returns>
        private LogData QueryLogDataValues(Log log, WitsmlQueryParser parser)
        {
            Tuple<double?, double?> range;
            var increasing = log.Direction != LogIndexDirection.decreasing;
            var isTimeLog = log.IndexType == LogIndexType.datetime;
            if (isTimeLog)
            {
                var startIndex = ToNullableUnixSeconds(parser.PropertyValue("startDateTimeIndex"));
                var endIndex = ToNullableUnixSeconds(parser.PropertyValue("endDateTimeIndex"));
                range = new Tuple<double?, double?>(startIndex, endIndex);
            }
            else
            {
                var startIndex = ToNullableDouble(parser.PropertyValue("startIndex"));
                var endIndex = ToNullableDouble(parser.PropertyValue("endIndex"));
                range = new Tuple<double?, double?>(startIndex, endIndex);
            }

            var logDataElement = parser.Property("logData");
            if (logDataElement == null)
                return null;

            var source = log.LogCurveInfo.Select(x => x.Mnemonic.ToString()).ToList();
            var target = logDataElement.Elements().FirstOrDefault(e => e.Name.LocalName == "mnemonicList").Value.Split(',');
            var mnemonics = target.Where(m => source.Contains(m)).ToList();

            var channelDataAdapter = new ChannelDataAdapter(DatabaseProvider);
            
            return channelDataAdapter.GetLogData(log.Uid, mnemonics, range, increasing);
        }

        /// <summary>
        /// Queries the log data data values for the range specified by the startIndex and endIndex parameters
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="startIndex">The start index value of the range.</param>
        /// <param name="endIndex">The end index value of the range.</param>
        /// <param name="inclusiveEnd">if set to <c>true</c> [inclusive end].</param>
        /// <returns>An <see cref="IMongoQueryable{LogDataValues}"/> instance for the requested range.</returns>
        private IMongoQueryable<LogDataValues> QueryLogDataValues(Log log, double? startIndex, double? endIndex, bool inclusiveEnd = true)
        {
            // Default to return all entities
            var query = GetQuery<LogDataValues>(DbCollectionNameLogDataValues)
                .Where(x => x.UidLog == log.Uid);

            // Filter by index ranges
            if (startIndex.HasValue)
            {
                query = query.Where(x => x.EndIndex >= startIndex.Value);
            }

            if (endIndex.HasValue)
            {
                if (inclusiveEnd)
                {
                    query = query.Where(x => x.StartIndex <= endIndex.Value);
                }
                else
                {
                    query = query.Where(x => x.StartIndex < endIndex.Value);
                }
            }

            return query;
        }

        /// <summary>
        /// Gets the mnemonic index positions of the "slice" the log data results.
        /// </summary>
        /// <param name="logMnemonics">An array of all log mnemonics.</param>
        /// <param name="queryMnemonics">A comma separated list of mnemonics requested in the query.</param>
        /// <returns>An integer array of the requested mnemonic indexes or null if all mnemonics are requested. </returns>
        private int[] GetSliceIndexes(string[] logMnemonics, string queryMnemonics)
        {
            if (logMnemonics != null && !string.IsNullOrEmpty(queryMnemonics))
            {
                var queryMnemonicsArray = queryMnemonics.Split(',');

                // If the lists are the same length then no slicing, return null;
                if (logMnemonics.Length == queryMnemonicsArray.Length)
                {
                    return null;
                }
                else
                {
                    return logMnemonics
                        .Select((x, i) => queryMnemonicsArray.Contains(logMnemonics[i]) ? i : -1)
                        .Where(x => x >= 0)
                        .ToArray();
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a string to a nullable double.
        /// </summary>
        /// <param name="doubleStr">The double string.</param>
        /// <returns>A <see cref="Nullable{double}"/> representation of the doubleStr parameter.</returns>
        private double? ToNullableDouble(string doubleStr)
        {
            return string.IsNullOrEmpty(doubleStr) ? (double?)null : double.Parse(doubleStr);
        }

        private double? ToNullableUnixSeconds(string dateTimeStr)
        {
            return string.IsNullOrEmpty(dateTimeStr)? (double?)null: DateTimeOffset.Parse(dateTimeStr).ToUnixTimeSeconds();
        }

        /// <summary>
        /// Extracts the reqeusted range and mnemonic slices of data from an <see cref="IEnumerable{LogDataValues}"/>.
        /// </summary>
        /// <param name="logDataValues">The log data values.</param>
        /// <param name="startIndex">The start index of the requested range.</param>
        /// <param name="endIndex">The end index of the requested range.</param>
        /// <param name="sliceIndexes">The indexes positions of the mnemonics to slice the log data.</param>
        /// <returns>An <see cref="IEnumerable{string}"/> of comma separated log data values for a given range and mnemonic slices.</returns>
        private IEnumerable<string> ToLogData(IEnumerable<LogDataValues> logDataValues, double? startIndex, double? endIndex, int[] sliceIndexes)
        {
            foreach (var ldv in logDataValues)
            {
                foreach (var item in GetSequence(ldv.Uid, ldv.Data.Split(';'), sliceIndexes))
                {
                    // Skip any records at the beginning that we don't want
                    if (startIndex.HasValue && item.Item2 < startIndex.Value)
                        continue;
                    else if (endIndex.HasValue && item.Item2 > endIndex.Value)
                        // If we've reached the end stop looping
                        break;
                    else
                        yield return item.Item3;
                }
            }
        }

        /// <summary>
        /// Extracts the log data data from the Log.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns>A string list of comma separated data that is the LogData.Data from the first LogData element of the log</returns>
        private List<string> ExtractLogDataData(Log log)
        {
            var logDataList = new List<string>();

            // if there is any log data copy it into a LogDataValues object and
            //... clear the LogData from the log.
            if (log.LogData != null && log.LogData.Count > 0)
            {
                var logData = log.LogData.FirstOrDefault();

                // If there is log data
                if (logData.Data.Count > 0)
                {
                    logDataList = logData.Data;
                }
                log.LogData.Clear();
            }

            return logDataList;
        }

        /// <summary>
        /// Combines a sequence of log data values into a "chunk" of data in a single <see cref="LogDataValues"/> instnace.
        /// </summary>
        /// <param name="sequence">An <see cref="IEnumerable{Tuple{string, double, string}}"/> containing a uid, the index value of the data and the row of data.</param>
        /// <returns>An <see cref="IEnumerable{LogDataValues}"/> of "chunked" data.</returns>
        private IEnumerable<LogDataValues> ToChunks(IEnumerable<Tuple<string, double, string>> sequence)
        {
            Tuple<int, int> plannedRange = null;
            double startIndex = 0;
            double endIndex = 0;
            StringBuilder data = new StringBuilder();
            string uid = string.Empty;

            foreach (var item in sequence)
            {
                if (plannedRange == null)
                {
                    plannedRange = ComputeRange(item.Item2, LogIndexRangeSize);
                    uid = item.Item1;
                    startIndex = item.Item2;
                }

                // If we're within the plannedRange append to the data and update the endIndex
                if (item.Item2 < plannedRange.Item2)
                {
                    // While still appending data for the current chunk set the uid if it is currently blank
                    uid = string.IsNullOrEmpty(uid) ? item.Item1 : uid;

                    data.Append(data.Length > 0 ? ";" : string.Empty).Append(item.Item3);
                    endIndex = item.Item2;
                }
                else
                {
                    yield return new LogDataValues() { StartIndex = startIndex, EndIndex = endIndex, Data = data.ToString(), Uid = uid };
                    plannedRange = ComputeRange(item.Item2, LogIndexRangeSize);
                    data.Length = 0;
                    data.Append(item.Item3);
                    startIndex = item.Item2;
                    endIndex = item.Item2;
                    uid = item.Item1;
                }
            }

            if (data.Length > 0)
            {
                yield return new LogDataValues() { StartIndex = startIndex, EndIndex = endIndex, Data = data.ToString(), Uid = uid };
            }
        }

        /// <summary>
        /// Builds a sequence of <see cref="Tuple{string, double, string}"/> from an <see cref="IEnumerable{string}"/> of log data.  
        /// </summary>
        /// <param name="uid">The uid.</param>
        /// <param name="dataList">The data list.</param>
        /// <param name="sliceIndexes">The menmonic indexes to slice the data for results.</param>
        /// <returns>An <see cref="IEnumerable{Tuple{string,double,string}}"/> sonsisting of a uid, the index value of the current row of data and the row of log data.</returns>
        private IEnumerable<Tuple<string, double, string>> GetSequence(string uid, IEnumerable<string> dataList, int[] sliceIndexes = null)
        {
            foreach(var dataRow in dataList)
            {
                // TODO: Validate the index order (is the data sorted?)
                var allValues = dataRow.Split(',');

                // If we're not slicing send all the data
                if (sliceIndexes == null)
                {
                    yield return new Tuple<string, double, string>(uid, double.Parse(allValues[0]), dataRow);
                }
                else
                {
                    var slicedData = string.Join(",", SliceStringList(allValues, sliceIndexes));
                    yield return new Tuple<string, double, string>(uid, double.Parse(allValues[0]), slicedData);
                }
            }
        }

        /// <summary>
        /// Slices a string list for the index values in the sliceIndexes parameter
        /// </summary>
        /// <param name="stringList">The string list to pull values from.</param>
        /// <param name="sliceIndexes">The indexes of the stringList we want values for.</param>
        /// <returns>An <see cref="IEnumerable{string}"/> for the slice indexes.</returns>
        private IEnumerable<string> SliceStringList(IEnumerable<string> stringList, int[] sliceIndexes)
        {
            if (sliceIndexes != null)
            {
                return stringList.Where((x, i) => sliceIndexes.Contains(i)).ToArray();
            }
            return stringList;
        }

        /// <summary>
        /// Writes an <see cref="IEnumerable{LogDataValues}"/> to the database belonging to the given log.
        /// </summary>
        /// <param name="log">The log containing the <see cref="LogDataValues"/>.</param>
        /// <param name="logDataValuesList">The log data values list.</param>
        private void WriteLogDataValues(Log log, IEnumerable<LogDataValues> logDataValuesList)
        {
            var collection = GetCollection<LogDataValues>(DbCollectionNameLogDataValues);

            collection.BulkWrite(logDataValuesList
                .Select(x =>
               {
                   if (string.IsNullOrEmpty(x.Uid))
                   {
                       x.UidLog = log.Uid;
                       x.Uid = NewUid();
                       return (WriteModel<LogDataValues>) new InsertOneModel<LogDataValues>(x);
                   }

                   var filter = Builders<LogDataValues>.Filter;
                   var update = Builders<LogDataValues>.Update;

                   return new UpdateOneModel<LogDataValues>(
                       filter.Eq(f => f.UidLog, log.Uid) & filter.Eq(f => f.Uid, x.Uid),
                       update
                           .Set(u => u.StartIndex, x.StartIndex)
                           .Set(u => u.EndIndex, x.EndIndex)
                           .Set(u => u.Data, x.Data));
               }));
        }

        /// <summary>
        /// Merges two sequences of log data to update a log data value "chunk"
        /// </summary>
        /// <param name="existingLogDataSequence">The existing log data sequence.</param>
        /// <param name="updateLogDataSequence">The update log data sequence.</param>
        /// <returns>The merged sequence of data</returns>
        private IEnumerable<Tuple<string, double, string>> MergeSequence(
            IEnumerable<Tuple<string, double, string>> existingLogDataSequence,
            IEnumerable<Tuple<string, double, string>> updateLogDataSequence)
        {
            string uid = string.Empty;

            using (var existingEnum = existingLogDataSequence.GetEnumerator())
            using (var updateEnum = updateLogDataSequence.GetEnumerator())
            {
                var endOfExisting = !existingEnum.MoveNext();
                var endOfUpdate = !updateEnum.MoveNext();

                while (!(endOfExisting && endOfUpdate))
                {
                    uid = endOfExisting ? string.Empty : existingEnum.Current.Item1;

                    if (!endOfExisting && (endOfUpdate || existingEnum.Current.Item2 < updateEnum.Current.Item2))
                    {
                        yield return existingEnum.Current;
                        endOfExisting = !existingEnum.MoveNext();
                    }
                    else
                    {
                        yield return new Tuple<string, double, string>(uid, updateEnum.Current.Item2, updateEnum.Current.Item3);
                        if (!endOfExisting && existingEnum.Current.Item2 == updateEnum.Current.Item2)
                        {
                            endOfExisting = !existingEnum.MoveNext();
                        }
                        endOfUpdate = !updateEnum.MoveNext();
                    }
                }
            }
        }

        /// <summary>
        /// Builds a sequence of <see cref="Tuple{string, double, string}"/> from an <see cref="IEnumerable{LogDataValues}"/>.
        /// </summary>
        /// <param name="logDataValues">The log data values.</param>
        /// <returns>An <see cref="IEnumerable{Tuple{string, double, string}}"/> containing a uid, index value of the data row and a single data row of values.</returns>
        private IEnumerable<Tuple<string, double, string>> ToSequence(IEnumerable<LogDataValues> logDataValues)
        {
            foreach (var ldv in logDataValues)
            {
                foreach (var item in GetSequence(ldv.Uid, ldv.Data.Split(';')))
                {
                    yield return item;
                }
            }
        }

        #region UpdateLogHeaderRanges Code
        //private void UpdateLogData(Log log, List<LogDataValues> logDataChanges)
        //{
        //    var database = DatabaseProvider.GetDatabase();
        //    var collection = database.GetCollection<LogDataValues>(_DbLogDataValuesDocumentName);
        //    var changeIndexes = logDataChanges.Select(x => x.Index);


        //    List<LogDataValues> newLogDataChanges;
        //    List<LogDataValues> updateLogDataChanges;

        //    // Pull existing indexes but only for those that are in our change list.
        //    var existingIndexes = collection.AsQueryable()
        //        .Where(x => x.UidLog == log.Uid && changeIndexes.Contains(x.Index))
        //        .Select(x => x.Index)
        //        .ToList();

        //    newLogDataChanges = logDataChanges.Where(ldc => !existingIndexes.Contains(ldc.Index)).ToList();
        //    updateLogDataChanges = logDataChanges.Where(ldc => existingIndexes.Contains(ldc.Index)).ToList();


        //    if (newLogDataChanges.Any())
        //    {
        //        //CreateLogDataValues(log, newLogDataChanges);
        //    }

        //    if (updateLogDataChanges.Any())
        //    {
        //        updateLogDataChanges.ForEach(ldc =>
        //        {
        //            var query = collection.AsQueryable()
        //                .Where(x => x.UidLog == log.Uid && x.Index == ldc.Index);

        //            var existingLogDataValues = query.FirstOrDefault();
        //            var updateFilter = Builders<LogDataValues>.Filter.Eq("Uid", existingLogDataValues.Uid);
        //            var update = Builders<LogDataValues>.Update.Set("Data", ldc.Data);
        //            collection.UpdateOne(updateFilter, update);
        //        });
        //    }

        //    // Update index range references within the log
        //    UpdateLogHeaderRanges(log);
        //}

        // TODO: Update later (the right way)
        //private void UpdateLogHeaderRanges(Log log)
        //{
        //    var database = DatabaseProvider.GetDatabase();
        //    var collection = database.GetCollection<Log>(_DbDocumentName);
        //    var updateFilter = Builders<Log>.Filter.Eq("Uid", log.Uid);

        //    // Find the Log that needs to be updated
        //    var dbLog = GetEntity(log.Uid, _DbDocumentName);

        //    // Get the min and max index range for this log.
        //    double startIndex;
        //    double endIndex;
        //    GetLogDataIndexRange(log, out startIndex, out endIndex);
        //    dbLog.StartIndex = UpdateGenericMeasure(dbLog.StartIndex, startIndex);
        //    dbLog.EndIndex = UpdateGenericMeasure(dbLog.EndIndex, endIndex);
        //    var update = Builders<Log>.Update.Set("StartIndex", dbLog.StartIndex);
        //    update.Set("EndIndex", dbLog.EndIndex);


        //    if (dbLog.LogCurveInfo != null)
        //    {
        //        dbLog.LogCurveInfo.ForEach(x =>
        //        {
        //            x.MinIndex = UpdateGenericMeasure(x.MinIndex, startIndex);
        //            x.MaxIndex = UpdateGenericMeasure(x.MaxIndex, endIndex);
        //        });
        //        update.Set("LogCurveInfo", dbLog.LogCurveInfo);
        //    }
        //    collection.UpdateOne(updateFilter, update);
        //}

        //private void GetLogDataIndexRange(Log log, out double startIndex, out double endIndex)
        //{
        //    var database = DatabaseProvider.GetDatabase();
        //    var collection = database.GetCollection<LogDataValues>(_DbLogDataValuesDocumentName);

        //    // Fetch the LogDataValue record for the Log with the smallest index
        //    var min = collection.AsQueryable()
        //        .Where(x => x.UidLog == log.Uid)
        //        .OrderBy(x => x.Index)
        //        .Select(x => x.Index)
        //        .Take(1).FirstOrDefault();

        //    var max = collection.AsQueryable()
        //        .Where(x => x.UidLog == log.Uid)
        //        .OrderByDescending(x => x.Index)
        //        .Select(x => x.Index)
        //        .Take(1).FirstOrDefault();

        //    // Initialize
        //    startIndex = min;
        //    endIndex = max;
        //}

        private GenericMeasure UpdateGenericMeasure(GenericMeasure gmObject, double gmValue)
        {
            if (gmObject == null)
            {
                gmObject = new GenericMeasure();
            }
            gmObject.Value = gmValue;

            return gmObject;
        }
        #endregion
    }
}
