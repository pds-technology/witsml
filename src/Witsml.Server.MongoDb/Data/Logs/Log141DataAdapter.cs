using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.Datatypes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Logs
{
    [Export(typeof(IWitsml141Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [Export(typeof(IEtpDataAdapter<Log>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log141DataAdapter : MongoDbDataAdapter<Log>, IWitsml141Configuration
    {
        private static readonly string DbCollectionNameLogDataValues = "logDataValues";
        private static readonly int LogIndexRangeSize = Settings.Default.LogIndexRangeSize;

        [ImportingConstructor]
        public Log141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Log141)
        {
        }

        public void GetCapabilities(CapServer capServer)
        {
            capServer.Add(Functions.GetFromStore, new ObjectWithConstraint(ObjectTypes.Log)
            {
                MaxDataNodes = 5000,
                MaxDataPoints = 10000
            });

            capServer.Add(Functions.AddToStore, ObjectTypes.Log);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Log);
            //capServer.Add(Functions.DeleteFromStore, ObjectTypes.Well);
        }

        /// <summary>
        /// Queries the object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>
        /// Queried objects.
        /// </returns>
        public override WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser)
        {
            List<string> fields = null;
            if (parser.ReturnElements() == OptionsIn.ReturnElements.IdOnly.Value)
                fields = new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" };

            var logs = QueryEntities<LogList>(parser, fields);

            // Support OptionsIn returnElements=: all, header-only, data-only
            var logsOut = new List<Log>();
            if (parser.ReturnElements() == OptionsIn.ReturnElements.DataOnly.Value)
            {
                logsOut.AddRange(GetLogHeaderRequired(logs));
            }
            else
            {
                logsOut.AddRange(logs);
            }

            // Only get the LogData returnElements != "header-only"
            if (parser.ReturnElements() != OptionsIn.ReturnElements.HeaderOnly.Value)
            {
                logsOut.ForEach(l =>
                {
                    l.LogData.Add(QueryLogDataValues(l, parser));
                });
            }

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                new LogList()
                {
                    Log = logsOut
                });
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

        private IEnumerable<Log> GetLogHeaderRequired(List<Log> logs)
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

        private LogData QueryLogDataValues(Log log, WitsmlQueryParser parser)
        {
            // Get the indexes to slice the log data by comparing the log's
            //... mnemonic list to the query's mnemonic list.
            var sliceIndexes = GetSliceIndexes(
                log.LogCurveInfo.Select(x => x.Mnemonic.ToString()).ToArray(),
                parser.PropertyValue(parser.Property("logData"), "mnemonicList"));

            // Create a LogData object to return with an empty Data list
            LogData logData = new LogData()
            {
                MnemonicList = string.Join(",", SliceStringList(
                    log.LogCurveInfo.Select(x => x.Mnemonic.ToString()),
                    sliceIndexes)),
                UnitList = string.Join(",", SliceStringList(
                    log.LogCurveInfo.Select(x => x.Unit),
                    sliceIndexes)),
                Data = new List<string>()
            };

            // Get a start and end index for index range filtering if supplied.
            double? startIndex = ToNullableDouble(parser.PropertyValue("startIndex"));
            double? endIndex = ToNullableDouble(parser.PropertyValue("endIndex"));

            IMongoQueryable<LogDataValues> query = QueryLogDataValues(log, startIndex, endIndex);

            // Get the Data for LogData
            logData.Data.AddRange(ToLogData(query.ToEnumerable(), startIndex, endIndex, sliceIndexes));

            return logData;
        }

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

        private double? ToNullableDouble(string doubleStr)
        {
            return string.IsNullOrEmpty(doubleStr) ? (double?)null : double.Parse(doubleStr);
        }

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

        public override WitsmlResult Add(Log entity)
        {
            entity.Uid = NewUid(entity.Uid);
            entity.CommonData = entity.CommonData.Update();

            var validator = Container.Resolve<IDataObjectValidator<Log>>();
            validator.Validate(Functions.AddToStore, entity);

            try
            {
                // Separate the LogData.Data from the Log
                var logData = ExtractLogData(entity);

                // Save the log and verify
                InsertEntity(entity);

                // If there is any LogData.Data then save it.
                if (logData.Any())
                {
                    WriteLogDataValues(entity, ToChunks(GetSequence(string.Empty, logData)));
                }

                return new WitsmlResult(ErrorCodes.Success, entity.Uid);
            }
            catch (Exception ex)
            {
                return new WitsmlResult(ErrorCodes.Unset, ex.Message + "\n" + ex.StackTrace);
            }
        }

        private List<string> ExtractLogData(Log log)
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

        private IEnumerable<string> SliceStringList(IEnumerable<string> stringList, int[] sliceIndexes)
        {
            if (sliceIndexes != null)
            {
                return stringList.Where((x, i) => sliceIndexes.Contains(i)).ToArray();
            }
            return stringList;
        }

        public Tuple<int, int> ComputeRange(double index, int rangeSize)
        {
            var rangeIndex = (int)(Math.Floor(index / rangeSize));
            return new Tuple<int, int>(rangeIndex * rangeSize, rangeIndex * rangeSize + rangeSize);

        }

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

        public override WitsmlResult Update(Log entity)
        {
            //List<LogDataValues> logDataValuesList = null;

            // Separate the LogData.Data from the Log
            var logData = ExtractLogData(entity);

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
