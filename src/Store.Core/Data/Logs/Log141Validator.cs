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
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Provides validation for <see cref="Log" /> data objects.
    /// </summary>
    public partial class Log141Validator
    {
        private readonly string[] _illegalColumnIdentifiers = { "'", "\"", "<", ">", "/", "\\", "&", "," };
        private static readonly string _dataDelimiterErrorMessage = WitsmlSettings.DataDelimiterErrorMessage;

        /// <summary>
        /// Gets or sets the Witsml configuration providers.
        /// </summary>
        [ImportMany]
        public IEnumerable<IWitsml141Configuration> Providers { get; set; }

        /// <summary>
        /// Configures the context.
        /// </summary>
        protected override void ConfigureContext()
        {
            Context.Ignored = new List<string>
            {
                "logData", "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex",
                "minIndex", "maxIndex", "minDateTimeIndex", "maxDateTimeIndex"
            };
        }

        /// <summary>
        /// Validates the data object while executing GetFromStore
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForGet()
        {
            // Only raise error -458 if requesting logData
            if (Parser.IncludeLogData() &&
                (Parser.HasPropertyValue("startIndex") || Parser.HasPropertyValue("endIndex")) &&
                (Parser.HasPropertyValue("startDateTimeIndex") || Parser.HasPropertyValue("endDateTimeIndex")))
            {
                yield return new ValidationResult(ErrorCodes.MixedStructuralRangeIndices.ToString(), 
                    new[] { "StartIndex", "EndIndex", "StartDateTimeIndex", "EndDateTimeIndex" });
            }

            var logDatas = Parser.Properties("logData").ToArray();
            if (logDatas.Length > 1)
            {
                yield return new ValidationResult(ErrorCodes.RecurringLogData.ToString(), new[] { "LogData" });
            }

            var mnemonics = Parser.GetLogDataMnemonics();
            var mnemonicList = mnemonics?.ToArray() ?? new string[0];

            if (logDatas.Length == 1)
            {
                if (mnemonicList.Any() && DuplicateUid(mnemonicList))
                {
                    yield return new ValidationResult(ErrorCodes.DuplicateMnemonics.ToString(), new[] { "LogData", "MnemonicsList" });
                }            
            }

            if (OptionsIn.ReturnElements.Requested.Equals(Parser.ReturnElements()))
            {
                var logCurveInfoMnemonics = Parser.GetLogCurveInfoMnemonics().ToList();                
                var logCurveInfos = Parser.Properties("logCurveInfo").ToArray();

                if (logCurveInfoMnemonics.Count != logCurveInfos.Length)
                {
                    yield return new ValidationResult(ErrorCodes.MissingMnemonicElement.ToString(), new[] { "LogCurveInfo", "Mnemonic" });
                }

                if (logDatas.Length == 1)
                {                 
                    if (logCurveInfoMnemonics.Any() && mnemonicList.Any() && !(logCurveInfoMnemonics.All(x => mnemonicList.Contains(x)) && mnemonicList.All(y => logCurveInfoMnemonics.Contains(y))))
                    {
                        yield return new ValidationResult(ErrorCodes.ColumnIdentifiersNotSame.ToString(), new[] { "LogData", "MnemonicList" });
                    }

                    if (mnemonics == null)
                    {
                        yield return new ValidationResult(ErrorCodes.MissingMnemonicList.ToString(), new[] { "LogData", "MnemonicsList" });
                    }
                }
            }
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            var logCurves = DataObject.LogCurveInfo;
            var indexCurve = DataObject.IndexCurve;

            var logDatas = DataObject.LogData;
            var logParams = DataObject.LogParam;
            var logCurveInfoMnemonics = new List<string>();

            logCurves?.ForEach(l => logCurveInfoMnemonics.Add(l.Mnemonic.Value));

            // Validate common attributes
            foreach (var result in base.ValidateForInsert())
                yield return result;

            // Validate that uid for LogParam exists
            if (logParams != null && logParams.Any(lp => string.IsNullOrWhiteSpace(lp.Uid)))
            {
                yield return new ValidationResult(ErrorCodes.MissingElementUidForAdd.ToString(), new[] { "LogParam", "Uid" });
            }

            // Validate for a bad column identifier in LogCurveInfo Mnemonics
            else if (_illegalColumnIdentifiers.Any(s => { return logCurves != null && logCurves.Any(m => m.Mnemonic.Value.Contains(s)); }))
            {
                yield return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogCurveInfo.Mnemonic" });
            }

            // Validate that uids in LogCurveInfo are unique
            else if (logCurves != null && logCurves.HasDuplicateUids())
            {
                yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "LogCurveInfo", "Uid" });
            }

            // Validate that uids in LogParam are unique
            else if (logParams != null && logParams.HasDuplicateUids())
            {
                yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "LogParam", "Uid" });
            }

            // Validate that column-identifiers in LogCurveInfo are unique
            else if (DuplicateColumnIdentifier())
            {
                yield return new ValidationResult(ErrorCodes.DuplicateColumnIdentifiers.ToString(), new[] { "LogCurveInfo", "Mnemonic" });
            }

            // Validate structural-range indices for consistent index types
            else if ((DataObject.StartIndex != null || DataObject.EndIndex != null) && (DataObject.StartDateTimeIndex != null || DataObject.EndDateTimeIndex != null))
            {
                yield return new ValidationResult(ErrorCodes.MixedStructuralRangeIndices.ToString(), new[] { "StartIndex", "EndIndex", "StartDateTimeIndex", "EndDateTimeIndex" });
            }

            // Validate that the dataDelimiter does not contain any white space
            else if (!DataObject.IsValidDataDelimiter())
            {
                yield return new ValidationResult(_dataDelimiterErrorMessage, new[] { "DataDelimiter" });
            }

            // Validate if MaxDataPoints has been exceeded
            else if (logDatas != null && logDatas.Count > 0 )
            {
                yield return
                    ValidateLogData(indexCurve, logCurves, logDatas, logCurveInfoMnemonics,
                        DataObject.GetDataDelimiterOrDefault(), Functions.AddToStore);
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
            // Validate Log uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) || string.IsNullOrWhiteSpace(DataObject.UidWellbore) || string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
            }
            else
            {
                var uri = DataObject.GetUri();
                var logCurves = DataObject.LogCurveInfo;
                var logParams = DataObject.LogParam;
                var logData = DataObject.LogData;

                var current = DataAdapter.Get(uri);
                var delimiter = current?.GetDataDelimiterOrDefault();

                var mergedLogCurveMnemonics = new List<string>();

                current?.LogCurveInfo.ForEach(l => mergedLogCurveMnemonics.Add(l.Mnemonic.Value));
                logCurves?.ForEach(l =>
                {
                    if (l.Mnemonic != null && !mergedLogCurveMnemonics.Contains(l.Mnemonic.Value))
                    {
                        mergedLogCurveMnemonics.Add(l.Mnemonic.Value);
                    }
                });

                // Validate Log does not exist
                if (current == null)
                {
                    yield return new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
                }

                // Validate that uid for LogCurveInfo exists
                else if (logCurves != null && logCurves.Any(l => string.IsNullOrWhiteSpace(l.Uid)))
                {
                    yield return new ValidationResult(ErrorCodes.MissingElementUidForUpdate.ToString(), new[] { "LogCurveInfo", "Uid" });
                }

                // Validate that uid for LogParam exists
                else if (logParams != null && logParams.Any(lp => string.IsNullOrWhiteSpace(lp.Uid)))
                {
                    yield return new ValidationResult(ErrorCodes.MissingElementUidForUpdate.ToString(), new[] { "LogParam", "Uid" });
                }

                // Validate that uids in LogCurveInfo are unique
                else if (logCurves != null && logCurves.HasDuplicateUids())
                {
                    yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "LogCurveInfo", "Uid" });
                }

                // Validate that uids in LogParam are unique
                else if (logParams != null && logParams.HasDuplicateUids())
                {
                    yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "LogParam", "Uid" });
                }

                else if (DuplicateColumnIdentifier())
                {
                    yield return new ValidationResult(ErrorCodes.DuplicateColumnIdentifiers.ToString(), new[] { "LogCurveInfo", "Mnemonic" });
                }

                // Validate that the dataDelimiter does not contain any white space
                else if (!DataObject.IsValidDataDelimiter())
                {
                    yield return new ValidationResult(_dataDelimiterErrorMessage, new[] { "DataDelimiter" });
                }

                // Validate LogCurveInfo
                else if (logCurves != null)
                {
                    var indexCurve = current.IndexCurve;
                    var isTimeLog = current.IsTimeLog(true);
                    var exist = current.LogCurveInfo ?? new List<LogCurveInfo>();
                    var hasLogData = logData != null && logData.Count > 0;
                    var existingCurves = exist.Select(e => e.Mnemonic.Value).ToArray();
                    var newCurves = logCurves.Where(l =>
                        !string.IsNullOrWhiteSpace(l.Mnemonic?.Value) &&
                        !existingCurves.ContainsIgnoreCase(l.Mnemonic.Value)).ToList();
                    var updateCurves = logCurves.Where(l =>
                        !string.IsNullOrWhiteSpace(l.Mnemonic?.Value) &&
                        !l.Mnemonic.Value.EqualsIgnoreCase(indexCurve) &&
                        existingCurves.ContainsIgnoreCase(l.Mnemonic.Value)).ToList();

                    if (hasLogData && newCurves.Count > 0 && updateCurves.Count > 0)
                    {
                        yield return new ValidationResult(ErrorCodes.AddingUpdatingLogCurveAtTheSameTime.ToString(), new[] { "LogCurveInfo", "Uid" });
                    }
                    else if (isTimeLog && newCurves.Any(c => c.MinDateTimeIndex.HasValue || c.MaxDateTimeIndex.HasValue)
                             || !isTimeLog && newCurves.Any(c => c.MinIndex != null || c.MaxIndex != null))
                    {
                        yield return new ValidationResult(ErrorCodes.IndexRangeSpecified.ToString(), new[] { "LogCurveInfo", "Index" });
                    }
                    else if (hasLogData)
                    {
                        yield return ValidateLogData(indexCurve, logCurves, logData, mergedLogCurveMnemonics, delimiter, Functions.UpdateInStore, false, existingCurves);
                    }
                }

                // TODO: check if this is still needed

                // Validate LogData
                else if (logData != null && logData.Count > 0)
                {
                    yield return ValidateLogData(current.IndexCurve, null, logData, mergedLogCurveMnemonics, delimiter, Functions.UpdateInStore, false);
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
            // Validate Log uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) || string.IsNullOrWhiteSpace(DataObject.UidWellbore) || string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(), new[] { "Uid", "UidWell", "UidWellbore" });
            }
            else
            {
                var uri = DataObject.GetUri();
                var logCurves = DataObject.LogCurveInfo;
                var logData = DataObject.LogData;
                var current = DataAdapter.Get(uri);

                // Validate Log does not exist
                if (current == null)
                {
                    yield return
                        new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(),
                            new[] {"Uid", "UidWell", "UidWellbore"});
                }
                else
                {
                    var element = Parser.Element();

                    // Validate deleting index curve
                    if (logCurves.Count > 0 && current.LogCurveInfo.Count > 0)
                    {
                        var indexCurve = current.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == current.IndexCurve);
                        var curveElements = Parser.Properties(element, "logCurveInfo").ToList();
                        var indexCurveElement = GetCurveElement(indexCurve, curveElements);

                        var emptyCurveUids = curveElements.Where(e => !e.HasElements)
                                    .Select(c => c.Attribute("uid")?.Value)
                                    .Where(v => !string.IsNullOrWhiteSpace(v))
                                    .ToList();

                        if (indexCurveElement != null)
                        {
                            if (!indexCurveElement.HasElements)
                            {
                                
                                if (current.LogCurveInfo.Select(l => l.Uid).Any(v => !emptyCurveUids.Contains(v)))
                                    yield return new ValidationResult(ErrorCodes.ErrorDeletingIndexCurve.ToString(), new[] { "LogCurveInfo" });
                            }
                            else
                            {
                                var isTimeLog = current.IsTimeLog();
                                var hasDefaultRange = isTimeLog
                                    ? DataObject.StartDateTimeIndex.HasValue || DataObject.EndDateTimeIndex.HasValue
                                    : DataObject.StartIndex != null || DataObject.EndIndex != null;

                                if (indexCurveElement.Elements().All(e => e.Name.LocalName == "mnemonic"))
                                {
                                    foreach (var curve in
                                            current.LogCurveInfo.Where(l => l.Mnemonic.Value != current.IndexCurve
                                                                            && !emptyCurveUids.Contains(l.Uid) && HasData(l, isTimeLog)))
                                    {
                                        var curveElement = GetCurveElement(curve, curveElements);
                                        if (curveElement == null)
                                            yield return new ValidationResult(ErrorCodes.ErrorDeletingIndexCurve.ToString(), new[] { "LogCurveInfo" });
                                        else
                                        {
                                            var curveInfo = GetCurveInfo(curve, logCurves);
                                            if (!ToDeleteCurveData(curveInfo, curveElement, isTimeLog, hasDefaultRange))
                                                yield return new ValidationResult(ErrorCodes.ErrorDeletingIndexCurve.ToString(), new[] { "LogCurveInfo" });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (logData != null && logData.Count > 0)
                    {
                        
                        var logDataElements = Parser.Properties(element, "logData");
                        if (logData.Count == 1)
                        {
                            var logDataElement = logDataElements.FirstOrDefault();
                            if (logDataElement != null)
                            {
                                if (logDataElement.HasElements)
                                {
                                    if (logDataElement.Elements().Any(e => e.Name.LocalName == "mnemonicList"))
                                        yield return
                                            new ValidationResult(ErrorCodes.ColumnIdentifierSpecified.ToString(),
                                                new[] { "LogData", "MnemonicList" });
                                    else
                                        yield return
                                            new ValidationResult(ErrorCodes.InputTemplateNonConforming.ToString(),
                                                new[] { "LogData" });
                                }
                                else
                                {
                                    yield return
                                            new ValidationResult(ErrorCodes.EmptyNonRecurringElementSpecified.ToString(),
                                                new[] { "LogData", "MnemonicList" });
                                }
                            }

                        }
                        else
                        {
                            yield return new ValidationResult(ErrorCodes.InputTemplateNonConforming.ToString(), new[] {"LogData"});
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validate the uid attribute value of the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The value of the uid attribute.</returns>
        /// <exception cref="WitsmlException">
        /// </exception>
        protected override string GetAndValidateArrayElementUid(XElement element)
        {
            var uidAttribute = element.Attributes().FirstOrDefault(a => a.Name == "uid");
            if (uidAttribute != null)
            {
                if (!string.IsNullOrEmpty(uidAttribute.Value))
                    return uidAttribute.Value;

                if (Context.Function != Functions.DeleteFromStore)
                    throw new WitsmlException(Context.Function.GetMissingElementUidErrorCode());
                throw new WitsmlException(ErrorCodes.EmptyUidSpecified);
            }
            if (Context.Function != Functions.DeleteFromStore)
                return null;
            if (element.Name.LocalName != "logCurveInfo" || !HasMnemonicElement(element))
                throw new WitsmlException(ErrorCodes.MissingElementUidForDelete);

            return null;
        }

        private bool HasMnemonicElement(XElement element)
        {
            return element.HasElements && element.Elements().Any(e => e.Name.LocalName.Equals("mnemonic"));
        }

        private bool DuplicateUid(IEnumerable<string> uids)
        {
            return uids.GroupBy(u => u)
                .Select(group => new { Uid = group.Key, Count = group.Count() })
                .Any(g => g.Count > 1);
        }

        private bool DuplicateColumnIdentifier()
        {
            var logCurves = DataObject.LogCurveInfo;
            if (logCurves == null || logCurves.Count == 0)
                return false;

            return logCurves.Where(l => l.Mnemonic != null).GroupBy(lci => lci.Mnemonic.Value)
                .Select(group => new { Mnemonic = group.Key, Count = group.Count() })
                .Any(g => g.Count > 1);
        }

        private bool UnitsMatch(List<LogCurveInfo> logCurves, LogData logData)
        {
            var mnemonics = ChannelDataReader.Split(logData.MnemonicList);
            var units = ChannelDataReader.Split(logData.UnitList);

            for (var i = 0; i < mnemonics.Length; i++)
            {
                var mnemonic = mnemonics[i];
                var logCurve = logCurves.FirstOrDefault(l => l.Mnemonic.Value.EqualsIgnoreCase(mnemonic));
                if (logCurve == null)
                    continue;

                if (string.IsNullOrEmpty(units[i].Trim()) && string.IsNullOrEmpty(logCurve.Unit) ||
                    units[i].Trim().EqualsIgnoreCase(logCurve.Unit))
                    continue;
                return false;
            }
            return true;
        }

        private ValidationResult ValidateLogData(string indexCurve, List<LogCurveInfo> logCurves, List<LogData> logDatas, List<string> mergedLogCurveInfoMnemonics, string delimiter, Functions function, bool insert = true, string[] existingMnemonics = null)
        {
            var totalPoints = 0;
            if (Context.Function.IsDataNodesValid(ObjectTypes.GetObjectType(DataObject), logDatas.Sum(x => x.Data.Count)))
            {
                return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] { "LogData", "Data" });
            }
            else
            {
                foreach (var logData in logDatas)
                {
                    if (string.IsNullOrWhiteSpace(logData.MnemonicList))
                        return new ValidationResult(ErrorCodes.MissingColumnIdentifiers.ToString(), new[] { "LogData", "MnemonicList" });
                    else
                    {
                        var mnemonics = ChannelDataReader.Split(logData.MnemonicList);
                        if (logData.Data != null && logData.Data.Count > 0)
                        {
                            // TODO: Optimize use of IsFirstValueDateTime inside of HasDuplicateIndexes (e.g. multiple calls to Split)
                            if (logData.Data.HasDuplicateIndexes(function, delimiter, logData.Data[0].IsFirstValueDateTime(delimiter), mnemonics.Length))
                            {
                                return new ValidationResult(ErrorCodes.NodesWithSameIndex.ToString(), new[] { "LogData", "Data" });
                            }

                            // TODO: Can we use mnemonics.Length instead of calling Split again here?
                            totalPoints += logData.Data.Count * ChannelDataReader.Split(logData.Data[0], delimiter).Length;
                        }
                        if (function.IsTotalDataPointsValid(totalPoints))
                        {
                            return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] { "LogData", "Data" });
                        }
                        else if (mnemonics.Distinct().Count() < mnemonics.Length)
                        {
                            return new ValidationResult(ErrorCodes.MnemonicsNotUnique.ToString(), new[] { "LogData", "MnemonicList" });
                        }
                        else if (mnemonics.Any(m => _illegalColumnIdentifiers.Any(c => m.Contains(c))))
                        {
                            return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogData", "MnemonicList" });
                        }
                        else if (logCurves?.Count > 0 && !IsValidLogDataMnemonics(logCurves.Select(x => x.Mnemonic.Value), mnemonics))
                        {
                            return new ValidationResult(ErrorCodes.MissingColumnIdentifiers.ToString(), new[] { "LogData", "MnemonicList" });
                        }
                        else if (existingMnemonics != null && !IsValidLogDataMnemonics(existingMnemonics, mnemonics))
                        {
                            return new ValidationResult(ErrorCodes.AddingUpdatingLogCurveAtTheSameTime.ToString(), new[] { "LogCurveInfo", "Uid" });
                        }
                        else if (!IsValidLogDataMnemonics(mergedLogCurveInfoMnemonics, mnemonics))
                        {
                            return new ValidationResult(ErrorCodes.MissingColumnIdentifiers.ToString(), new[] { "LogData", "MnemonicList" });
                        }
                        else if (insert && logCurves != null && mnemonics.Length > logCurves.Count)
                        {
                            return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogData", "MnemonicList" });
                        }
                        else if (string.IsNullOrWhiteSpace(logData.UnitList))
                        {
                            return new ValidationResult(ErrorCodes.MissingUnitList.ToString(), new[] { "LogData", "UnitList" });
                        }
                        else if (!UnitSpecified(logCurves, logData))
                        {
                            return new ValidationResult(ErrorCodes.MissingUnitForMeasureData.ToString(), new[] { "LogData", "UnitList" });
                        }
                        else if (!string.IsNullOrEmpty(indexCurve) && mnemonics.All(m => m != indexCurve))
                        {
                            return new ValidationResult(ErrorCodes.IndexCurveNotFound.ToString(), new[] { "IndexCurve" });
                        }
                        else if (!mnemonics[0].EqualsIgnoreCase(indexCurve))
                        {
                            return new ValidationResult(ErrorCodes.IndexNotFirstInDataColumnList.ToString(), new[] { "LogData", "MnemonicList" });
                        }
                        else if (DuplicateUid(mnemonics))
                        {
                            return new ValidationResult(ErrorCodes.MnemonicsNotUnique.ToString(), new[] { "LogData", "MnemonicList" });
                        }
                        else if (logCurves != null && !UnitsMatch(logCurves, logData))
                        {
                            return new ValidationResult(ErrorCodes.UnitListNotMatch.ToString(), new[] { "LogData", "UnitList" });
                        }
                    }
                }
            }

            return null;
        }

        private bool IsValidLogDataMnemonics(IEnumerable<string> logCurveInfoMnemonics, IEnumerable<string> logDataMnemonics)
        {
            Logger.Debug("Validating mnemonic list channels for existence in LogCurveInfo.");

            var isValid = logDataMnemonics.All(logCurveInfoMnemonics.ContainsIgnoreCase);

            Logger.Debug(isValid
                ? "Validation of mnemonic list channels successful."
                : "Mnemonic from mnemonic list does not exist in LogCurveInfo.");

            return isValid;
        }

        private bool UnitSpecified(List<LogCurveInfo> logCurves, LogData logData)
        {
            var mnemonics = ChannelDataReader.Split(logData.MnemonicList);
            var units = ChannelDataReader.Split(logData.UnitList);

            for (var i = 0; i < mnemonics.Length; i++)
            {
                var mnemonic = mnemonics[i];
                var logCurve = logCurves.FirstOrDefault(l => l.Mnemonic.Value.EqualsIgnoreCase(mnemonic));

                // If there are not enough units to cover all of the mnemonics OR 
                //... the LogCurve has a unit and the unit is empty the unit is not specified.
                if (units.Length <= i || (!string.IsNullOrEmpty(logCurve?.Unit) && string.IsNullOrEmpty(units[i].Trim())))
                    return false;
            }

            return true;
        }

        private bool HasData(LogCurveInfo curve, bool isTimeLog)
        {
            if (isTimeLog)
                return curve.MinDateTimeIndex.HasValue && curve.MaxDateTimeIndex.HasValue;

            return curve.MinIndex != null && curve.MaxIndex != null;
        }

        private bool ToDeleteCurveData(LogCurveInfo curve, XElement element, bool isTimeLog, bool hasDefaultRange)
        {
            var hasRange = isTimeLog
                ? curve.MinDateTimeIndex.HasValue || curve.MaxDateTimeIndex.HasValue
                : curve.MinIndex != null || curve.MaxIndex != null;

            if (hasRange)
                return true;

            if (string.IsNullOrWhiteSpace(element.Attribute("uid")?.Value))
            {
                if (element.Elements().All(e => e.Name.LocalName == "mnemonic"))
                    return true;
            }
            else
            {
                return hasDefaultRange;
            }

            return false;
        }

        private XElement GetCurveElement(LogCurveInfo curve, List<XElement> elements)
        {
            return elements.FirstOrDefault(e => e.Attribute("uid")?.Value == curve.Uid
                                                || e.Elements().Any(c => c.Name.LocalName == "mnemonic" && c.Value == curve.Mnemonic.Value));
        }

        private LogCurveInfo GetCurveInfo(LogCurveInfo curve, List<LogCurveInfo> curves)
        {
            return curves.FirstOrDefault(l => l.Uid == curve.Uid || l.Mnemonic.Value == curve.Mnemonic.Value);
        }
    }
}
