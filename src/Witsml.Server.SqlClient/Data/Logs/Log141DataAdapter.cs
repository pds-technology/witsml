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
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Logs
{
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [Export(typeof(IWitsml141Configuration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log141DataAdapter : SqlWitsmlDataAdapter<Log>, IWitsml141Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Log141DataAdapter" /> class.
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
            if (!DatabaseProvider.SchemaMapper.IsAvailable(ObjectName))
                return;

            var dataObject = new ObjectWithConstraint(ObjectTypes.Log)
            {
                MaxDataNodes = WitsmlSettings.MaxDataNodes,
                MaxDataPoints = WitsmlSettings.MaxDataPoints
            };

            capServer.Add(Functions.GetFromStore, dataObject);
            //capServer.Add(Functions.AddToStore, dataObject);
            //capServer.Add(Functions.UpdateInStore, dataObject);
            //capServer.Add(Functions.DeleteFromStore, ObjectTypes.Log);
        }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<Log> Query(WitsmlQueryParser parser, ResponseContext context)
        {
            var logs = QueryEntities(parser);

            if (parser.IncludeLogData())
            {
                ValidateGrowingObjectDataRequest(parser, logs);

                logs.ForEach(l =>
                {
                    var logHeader = GetLogHeader(l);
                    var mnemonics = GetMnemonicList(logHeader, parser);

                    QueryLogDataValues(l, logHeader, parser, mnemonics);
                    FormatLogHeader(l, mnemonics);
                });
            }
            else if (!OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                logs.ForEach(l =>
                {
                    var logHeader = GetLogHeader(l);
                    var mnemonics = GetMnemonicList(logHeader, parser);
                    FormatLogHeader(l, mnemonics);
                });
            }

            return logs;
        }

        /// <summary>
        /// Gets a list of the property names to project during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of property names.</returns>
        protected override List<string> GetProjectionPropertyNames(WitsmlQueryParser parser)
        {
            var mapping = DatabaseProvider.SchemaMapper.Schema.Mappings[ObjectName.Name];
            var returnElements = parser.ReturnElements();

            return OptionsIn.ReturnElements.IdOnly.Equals(returnElements)
                ? new List<string> { "Uid", "Name", "UidWell", "NameWell", "UidWellbore", "NameWellbore" }
                : OptionsIn.ReturnElements.DataOnly.Equals(returnElements)
                ? new List<string> { "Uid", "UidWell", "UidWellbore", "LogData" }
                //: OptionsIn.ReturnElements.Requested.Equals(returnElements)
                //? new List<string>() 
                : mapping.Columns
                    .Select(x => x.GetName())
                    .Where(x => !"LogData".EqualsIgnoreCase(x) || parser.IncludeLogData())
                    .ToList();
                //: null;
        }

        /// <summary>
        /// Gets a list of the element names to ignore during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForQuery(WitsmlQueryParser parser)
        {
            return new List<string> { "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex", "logData" };
        }

        /// <summary>
        /// Creates the query template.
        /// </summary>
        /// <returns>A query template.</returns>
        protected override WitsmlQueryTemplate<Log> CreateQueryTemplate()
        {
            return new WitsmlQueryTemplate<Log>(
                new Log()
                {
                    UidWell = "abc",
                    NameWell = "abc",
                    UidWellbore = "abc",
                    NameWellbore = "abc",
                    Uid = "abc",
                    Name = "abc"
                });
        }

        private Log GetLogHeader(Log log)
        {
            return log;
        }

        private IDictionary<int, string> GetMnemonicList(object log, WitsmlQueryParser parser)
        {
            return new Dictionary<int, string>();
        }

        private void QueryLogDataValues(Log log, Log logHeader, WitsmlQueryParser parser, IDictionary<int, string> mnemonics)
        {
        }

        private void FormatLogHeader(Log log, IDictionary<int, string> mnemonics)
        {
        }
    }
}
