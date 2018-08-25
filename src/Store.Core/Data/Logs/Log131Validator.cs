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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Provides validation for <see cref="Log" /> data objects.
    /// </summary>
    public partial class Log131Validator
    {
        private readonly string[] _illegalColumnIdentifiers = { "'", "\"", "<", ">", "/", "\\", "&", "," };

        /// <summary>
        /// Gets or sets the Witsml configuration providers.
        /// </summary>
        [ImportMany]
        public IEnumerable<IWitsml131Configuration> Providers { get; set; }

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

            var logCurveInfoMnemonics = Parser.GetLogCurveInfoMnemonics().ToList();
            var mnemonicList = logCurveInfoMnemonics.ToArray();

            if (logDatas.Length == 1)
            {
                if (mnemonicList.Any() && DuplicateUid(mnemonicList))
                {
                    yield return new ValidationResult(ErrorCodes.DuplicateMnemonics.ToString(), new[] { "LogData", "MnemonicsList" });
                }
            }

            if (OptionsIn.ReturnElements.Requested.Equals(Parser.ReturnElements()))
            {
                var logCurveInfos = Parser.Properties("logCurveInfo").ToArray();

                if (logCurveInfoMnemonics.Count != logCurveInfos.Length)
                {
                    yield return new ValidationResult(ErrorCodes.MissingMnemonicElement.ToString(), new[] { "LogCurveInfo", "Mnemonic" });
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
            var logCurveInfoMnemonics = new List<string>();

            logCurves?.ForEach(l => logCurveInfoMnemonics.Add(l.Mnemonic));

            // Validate common attributes
            foreach (var result in base.ValidateForInsert())
                yield return result;

            // Validate that uid for LogParam exists
            if (DataObject.LogParam != null && DataObject.LogParam.Any(lp => string.IsNullOrWhiteSpace(lp.Index.ToString())))
            {
                yield return new ValidationResult(ErrorCodes.MissingElementUidForAdd.ToString(), new[] { "LogParam", "Uid" });
            }

            // Validate for a bad column identifier in LogCurveInfo Mnemonics
            else if (_illegalColumnIdentifiers.Any(s => { return logCurves != null && logCurves.Any(m => m.Mnemonic.Contains(s)); }))
            {
                yield return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogCurveInfo.Mnemonic" });
            }

            // Validate that uids in LogCurveInfo are unique
            else if (logCurves != null && logCurves.HasDuplicateUids())
            {
                yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "LogCurveInfo", "Uid" });
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

            // Validate if MaxDataPoints has been exceeded
            else if (logDatas != null && logDatas.Count > 0)
            {
                yield return ValidateLogData(Functions.AddToStore, indexCurve.Value, logCurves, logDatas, logCurveInfoMnemonics, ",");
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
                var delimiter = ",";

                var mergedLogCurveMnemonics = new List<string>();

                current?.LogCurveInfo.ForEach(l => mergedLogCurveMnemonics.Add(l.Mnemonic));
                logCurves?.ForEach(l =>
                {
                    if (l.Mnemonic != null && !mergedLogCurveMnemonics.Contains(l.Mnemonic))
                    {
                        mergedLogCurveMnemonics.Add(l.Mnemonic);
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
                else if (logParams != null && logParams.Any(lp => string.IsNullOrWhiteSpace(lp.Index.ToString())))
                {
                    yield return new ValidationResult(ErrorCodes.MissingElementUidForUpdate.ToString(), new[] { "LogParam", "Uid" });
                }

                // Validate that uids in LogCurveInfo are unique
                else if (logCurves != null && logCurves.HasDuplicateUids())
                {
                    yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "LogCurveInfo", "Uid" });
                }

                // Validate that uids in LogParam are unique
                else if (logParams != null && DuplicateUid(logParams.Select(l => l.Index.ToString())))
                {
                    yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "LogParam", "Uid" });
                }

                else if (DuplicateColumnIdentifier())
                {
                    yield return new ValidationResult(ErrorCodes.DuplicateColumnIdentifiers.ToString(), new[] { "LogCurveInfo", "Mnemonic" });
                }

                // Validate LogCurveInfo
                else if (logCurves != null)
                {
                    var indexCurve = current.IndexCurve.Value;
                    var isTimeLog = current.IsTimeLog(true);
                    var exist = current.LogCurveInfo ?? new List<LogCurveInfo>();
                    var existingCurves = exist.Select(e => e.Mnemonic).ToList();
                    var newCurves = logCurves.Where(l =>
                            !l.Mnemonic.EqualsIgnoreCase(indexCurve) &&
                            !existingCurves.ContainsIgnoreCase(l.Mnemonic))
                        .ToList();

                    if (isTimeLog && newCurves.Any(c => c.MinDateTimeIndex.HasValue || c.MaxDateTimeIndex.HasValue)
                        || !isTimeLog && newCurves.Any(c => c.MinIndex != null || c.MaxIndex != null))
                    {
                        yield return new ValidationResult(ErrorCodes.IndexRangeSpecified.ToString(), new[] { "LogCurveInfo", "Index" });
                    }
                    else if (logData != null && logData.Count > 0)
                    {
                        yield return ValidateLogData(Functions.UpdateInStore, indexCurve, logCurves, logData, mergedLogCurveMnemonics, delimiter, false);
                    }
                }

                // TODO: check if this is still needed

                // Validate LogData
                else if (logData != null && logData.Count > 0)
                {
                    yield return ValidateLogData(Functions.UpdateInStore, current.IndexCurve.Value, null, logData, mergedLogCurveMnemonics, delimiter, false);
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
            if (string.IsNullOrWhiteSpace(DataObject.UidWell) || string.IsNullOrWhiteSpace(DataObject.UidWellbore) ||
                string.IsNullOrWhiteSpace(DataObject.Uid))
            {
                yield return
                    new ValidationResult(ErrorCodes.DataObjectUidMissing.ToString(),
                        new[] {"Uid", "UidWell", "UidWellbore"});
            }
            else
            {
                var uri = DataObject.GetUri();
                var logCurves = DataObject.LogCurveInfo;
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
                        var indexCurve = current.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == current.IndexCurve.Value);
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
                                    yield return
                                        new ValidationResult(ErrorCodes.ErrorDeletingIndexCurve.ToString(),
                                            new[] {"LogCurveInfo"});
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
                                        current.LogCurveInfo.Where(l => l.Mnemonic != current.IndexCurve.Value
                                                                        && !emptyCurveUids.Contains(l.Uid) &&
                                                                        HasData(l, isTimeLog)))
                                    {
                                        var curveElement = GetCurveElement(curve, curveElements);
                                        if (curveElement == null)
                                            yield return
                                                new ValidationResult(ErrorCodes.ErrorDeletingIndexCurve.ToString(),
                                                    new[] {"LogCurveInfo"});
                                        else
                                        {
                                            var curveInfo = GetCurveInfo(curve, logCurves);
                                            if (!ToDeleteCurveData(curveInfo, curveElement, isTimeLog, hasDefaultRange))
                                                yield return
                                                    new ValidationResult(ErrorCodes.ErrorDeletingIndexCurve.ToString(),
                                                        new[] {"LogCurveInfo"});
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    // Check to see if logData was specfied
                    if (Parser.HasElements("logData"))
                    {
                        yield return
                            new ValidationResult(ErrorCodes.EmptyNonRecurringElementSpecified.ToString(),
                                new[] { "LogData" });
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
            if (element.Name.LocalName != "logCurveInfo" || !DeleteChannelData(element))
                throw new WitsmlException(ErrorCodes.MissingElementUidForDelete);

            return null;
        }

        private bool DeleteChannelData(XElement element)
        {
            var fields = new List<string> { "mnemonic", "minDateTimeIndex", "maxDateTimeIndex", "minIndex", "maxIndex" };
            if (!element.HasElements)
                return false;

            return element.Elements().All(e => fields.Contains(e.Name.LocalName));
        }

        private ValidationResult ValidateLogData(Functions function, string indexCurve, List<LogCurveInfo> logCurves, List<string> logDatas, List<string> mergedLogCurveInfoMnemonics, string delimiter, bool insert = true)
        {
            // Validate that all logCurveInfos have columnIndex
            if (logCurves.Any(x => !x.ColumnIndex.HasValue))
                return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogCurveInfo" });

            // Validate there are no duplicate columnIndexes
            if (logCurves.GroupBy(x => x.ColumnIndex.Value).SelectMany(x => x.Skip(1)).Any())
                return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogCurveInfo" });

            // Validate there are no duplicate indexes
            // TODO: Optimize use of IsFirstValueDateTime inside of HasDuplicateIndexes (e.g. multiple calls to Split)
            if (logDatas.HasDuplicateIndexes(function, delimiter, logDatas[0].IsFirstValueDateTime(), logCurves.Count)) 
            {
                return new ValidationResult(ErrorCodes.NodesWithSameIndex.ToString(), new[] { "LogData", "Data" });
            }

            if (Context.Function.IsDataNodesValid(ObjectTypes.GetObjectType(DataObject), logDatas.Count))
            {
                return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] {"LogData", "Data"});
            }
            else
            {
                // TODO: Do we need to use ChannelDataReader.Split here or can we use logCurves.Count?
                var logDataColumnLength = logDatas[0].Split(',').Length;
                var totalPoints = logDatas.Count * logDataColumnLength;

                if (function.IsTotalDataPointsValid(totalPoints))
                {
                    return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] { "LogData", "Data" });
                }
                else if (ColumnIndexGreaterThanLength(logCurves, logDataColumnLength))
                {
                    return new ValidationResult(ErrorCodes.InvalidLogCurveInfoColumnIndex.ToString(), new[] { "LogCurveInfo" });
                }

                else if (mergedLogCurveInfoMnemonics.Distinct().Count() < mergedLogCurveInfoMnemonics.Count)
                {
                    return new ValidationResult(ErrorCodes.MnemonicsNotUnique.ToString(),
                        new[] { "LogCurveInfo" });
                }
                else if (mergedLogCurveInfoMnemonics.Any(m => _illegalColumnIdentifiers.Any(c => m.Contains(c))))
                {
                    return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(),
                        new[] {"LogCurveInfo"});
                }
                else if (insert && logCurves != null && mergedLogCurveInfoMnemonics.Count > logCurves.Count)
                {
                    return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(),
                        new[] {"LogCurveInfo"});
                }
                else if (!string.IsNullOrEmpty(indexCurve) && mergedLogCurveInfoMnemonics.All(m => m != indexCurve))
                {
                    return new ValidationResult(ErrorCodes.IndexCurveNotFound.ToString(), new[] {"IndexCurve"});
                }
                else if (!mergedLogCurveInfoMnemonics[0].EqualsIgnoreCase(indexCurve))
                {
                    return new ValidationResult(ErrorCodes.IndexNotFirstInDataColumnList.ToString(),
                        new[] {"LogCurveInfo"});
                }
                else if (DuplicateUid(mergedLogCurveInfoMnemonics))
                {
                    return new ValidationResult(ErrorCodes.MnemonicsNotUnique.ToString(),
                        new[] {"LogCurveInfo"});
                }

            }

            return null;
        }

        private bool HasData(LogCurveInfo curve, bool isTimeLog)
        {
            if (isTimeLog)
                return curve.MinDateTimeIndex.HasValue && curve.MaxDateTimeIndex.HasValue;

            return curve.MinIndex != null && curve.MaxIndex != null;
        }

        private bool ColumnIndexGreaterThanLength(List<LogCurveInfo> logCurves, int logDataColumnLength)
        {
            return logCurves.Any(c => c.ColumnIndex > logDataColumnLength);
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

            return logCurves.Where(l => l.Mnemonic != null).GroupBy(lci => lci.Mnemonic)
                .Select(group => new { Mnemonic = group, Count = group.Count() })
                .Any(g => g.Count > 1);
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
                                                || e.Elements().Any(c => c.Name.LocalName == "mnemonic" && c.Value == curve.Mnemonic));
        }

        private LogCurveInfo GetCurveInfo(LogCurveInfo curve, List<LogCurveInfo> curves)
        {
            return curves.FirstOrDefault(l => l.Uid == curve.Uid || l.Mnemonic == curve.Mnemonic);
        }
    }
}
