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
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using PDS.Framework;
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Provides validation for <see cref="Log" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Log}" />
    [Export(typeof(IDataObjectValidator<Log>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Log131Validator : DataObjectValidator<Log>
    {
        private readonly IWitsmlDataAdapter<Log> _logDataAdapter;
        private readonly IWitsmlDataAdapter<Wellbore> _wellboreDataAdapter;
        private readonly IWitsmlDataAdapter<Well> _wellDataAdapter;

        private readonly string[] _illegalColumnIdentifiers = { "'", "\"", "<", ">", "/", "\\", "&", "," };

        /// <summary>
        /// Initializes a new instance of the <see cref="Log131Validator" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="logDataAdapter">The log data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        [ImportingConstructor]
        public Log131Validator(IContainer container, IWitsmlDataAdapter<Log> logDataAdapter, IWitsmlDataAdapter<Wellbore> wellboreDataAdapter, IWitsmlDataAdapter<Well> wellDataAdapter) : base(container)
        {
            _logDataAdapter = logDataAdapter;
            _wellboreDataAdapter = wellboreDataAdapter;
            _wellDataAdapter = wellDataAdapter;

            Context.Ignored = new List<string> {"logData", "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex",
                "minIndex", "maxIndex", "minDateTimeIndex", "maxDateTimeIndex", };
        }

        /// <summary>
        /// Gets or sets the Witsml configuration providers.
        /// </summary>
        /// <value>
        /// The providers.
        /// </value>
        [ImportMany]
        public IEnumerable<IWitsml131Configuration> Providers { get; set; }

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

                if (logCurveInfoMnemonics.Count() != logCurveInfos.Length)
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
            var uri = DataObject.GetUri();
            var uriWellbore = uri.Parent;
            var uriWell = uriWellbore.Parent;
            var wellbore = _wellboreDataAdapter.Get(uriWellbore);
            var indexCurve = DataObject.IndexCurve;

            var logDatas = DataObject.LogData;
            var logCurveInfoMnemonics = new List<string>();

            logCurves?.ForEach(l => logCurveInfoMnemonics.Add(l.Mnemonic));

            // Validate parent uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell))
            {
                yield return new ValidationResult(ErrorCodes.MissingElementUidForAdd.ToString(), new[] { "UidWell" });
            }
            // Validate parent uid property
            else if (string.IsNullOrWhiteSpace(DataObject.UidWellbore))
            {
                yield return new ValidationResult(ErrorCodes.MissingElementUidForAdd.ToString(), new[] { "UidWellbore" });
            }

            // Validate parent exists
            else if (!_wellDataAdapter.Exists(uriWell))
            {
                yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWell" });
            }
            // Validate parent exists
            else if (wellbore == null)
            {
                yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWellbore" });
            }

            else if (!wellbore.UidWell.Equals(DataObject.UidWell) || !wellbore.Uid.Equals(DataObject.UidWellbore))
            {
                yield return new ValidationResult(ErrorCodes.IncorrectCaseParentUid.ToString(), new[] { "UidWellbore" });
            }

            // Validate UID does not exist
            else if (_logDataAdapter.Exists(uri))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            }

            // Validate that uid for LogParam exists
            else if (DataObject.LogParam != null && DataObject.LogParam.Any(lp => string.IsNullOrWhiteSpace(lp.Index.ToString())))
            {
                yield return new ValidationResult(ErrorCodes.MissingElementUidForAdd.ToString(), new[] { "LogParam", "Uid" });
            }

            // Validate for a bad column identifier in LogCurveInfo Mnemonics
            else if (_illegalColumnIdentifiers.Any(s => { return logCurves != null && logCurves.Any(m => m.Mnemonic.Contains(s)); }))
            {
                yield return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(), new[] { "LogCurveInfo.Mnemonic" });
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
                yield return ValidateLogData(indexCurve.Value, logCurves, logDatas, logCurveInfoMnemonics, ",");
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

                var current = _logDataAdapter.Get(uri);
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
                else if (logCurves != null && DuplicateUid(logCurves.Select(l => l.Uid)))
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
                    var indexCurve = current.IndexCurve;
                    var indexCurveUid = current.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == indexCurve.Value)?.Uid;
                    var isTimeLog = current.IsTimeLog(true);
                    var exist = current.LogCurveInfo ?? new List<LogCurveInfo>();
                    var uids = exist.Select(e => e.Uid.ToUpper()).ToList();
                    var newCurves = logCurves.Where(l => !uids.Contains(l.Uid.ToUpper())).ToList();
                    var updateCurves = logCurves.Where(l => !l.Uid.EqualsIgnoreCase(indexCurveUid) && uids.Contains(l.Uid.ToUpper())).ToList();

                    if (newCurves.Count > 0 && updateCurves.Count > 0)
                    {
                        yield return new ValidationResult(ErrorCodes.AddingUpdatingLogCurveAtTheSameTime.ToString(), new[] { "LogCurveInfo", "Uid" });
                    }
                    else if (isTimeLog && newCurves.Any(c => c.MinDateTimeIndex.HasValue || c.MaxDateTimeIndex.HasValue)
                        || !isTimeLog && newCurves.Any(c => c.MinIndex != null || c.MaxIndex != null))
                    {
                        yield return new ValidationResult(ErrorCodes.IndexRangeSpecified.ToString(), new[] { "LogCurveInfo", "Index" });
                    }
                    else if (logData != null && logData.Count > 0)
                    {
                        yield return ValidateLogData(indexCurve.Value, logCurves, logData, mergedLogCurveMnemonics, delimiter, false);
                    }
                }

                // TODO: check if this is still needed

                // Validate LogData
                else if (logData != null && logData.Count > 0)
                {
                    yield return ValidateLogData(current.IndexCurve.Value, null, logData, mergedLogCurveMnemonics, delimiter, false);
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

                var current = _logDataAdapter.Get(uri);

                // Validate Log does not exist
                if (current == null)
                {
                    yield return
                        new ValidationResult(ErrorCodes.DataObjectNotExist.ToString(),
                            new[] { "Uid", "UidWell", "UidWellbore" });
                }
                else
                {
                    // Validate deleting index curve
                    if (logCurves.Count > 0 && current.LogCurveInfo.Count > 0)
                    {
                        var indexCurve = current.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == current.IndexCurve.Value);
                        if (indexCurve != null && current.LogCurveInfo.Count == 1)
                            yield return new ValidationResult(ErrorCodes.ErrorDeletingIndexCurve.ToString(), new[] {"LogCurveInfo"});
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

        private ValidationResult ValidateLogData(string indexCurve, List<LogCurveInfo> logCurves, List<string> logDatas, List<string> mergedLogCurveInfoMnemonics, string delimiter, bool insert = true)
        {


            if (logDatas.Count() > WitsmlSettings.MaxDataNodes)
            {
                return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] {"LogData", "Data"});
            }
            else
            {
                var logDataColumnLength = logDatas[0].Split(',').Length;
                var totalPoints = logDatas.Count() * logDataColumnLength;

                if (totalPoints > WitsmlSettings.MaxDataPoints)
                {
                    return new ValidationResult(ErrorCodes.MaxDataExceeded.ToString(), new[] {"LogData", "Data"});
                }
                else if (ColumnIndexGreaterThanLength(logCurves, logDataColumnLength))
                {
                    return new ValidationResult(ErrorCodes.InvalidLogCurveInfoColumnIndex.ToString(), new[] { "LogCurveInfo" });
                }

                else if (mergedLogCurveInfoMnemonics.Distinct().Count() < mergedLogCurveInfoMnemonics.Count())
                {
                    return new ValidationResult(ErrorCodes.MnemonicsNotUnique.ToString(),
                        new[] { "LogCurveInfo" });
                }
                else if (mergedLogCurveInfoMnemonics.Any(m => _illegalColumnIdentifiers.Any(c => m.Contains(c))))
                {
                    return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(),
                        new[] {"LogCurveInfo"});
                }
                else if (insert && logCurves != null && mergedLogCurveInfoMnemonics.Count() > logCurves.Count)
                {
                    return new ValidationResult(ErrorCodes.BadColumnIdentifier.ToString(),
                        new[] {"LogCurveInfo"});
                }
                else if (!UnitSpecified(logCurves))
                {
                    return new ValidationResult(ErrorCodes.MissingUnitForMeasureData.ToString(),
                        new[] { "LogCurveInfo" });
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

        private bool ColumnIndexGreaterThanLength(List<LogCurveInfo> logCurves, int logDataColumnLength)
        {
            return logCurves.Any(c => c.ColumnIndex > logDataColumnLength);
        }

        private bool UnitSpecified(List<LogCurveInfo> logCurves)
        {
            return logCurves.All(lc => !string.IsNullOrWhiteSpace(lc.Unit));
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
    }
}
