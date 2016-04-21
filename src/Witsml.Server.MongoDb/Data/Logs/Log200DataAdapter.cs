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
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Log}" />
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log200DataAdapter : MongoDbDataAdapter<Log>
    {
        private readonly IWitsmlDataAdapter<ChannelSet> _channelSetDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log200DataAdapter" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="channelSetDataAdapter">The channel set data adapter.</param>
        [ImportingConstructor]
        public Log200DataAdapter(IDatabaseProvider databaseProvider, IWitsmlDataAdapter<ChannelSet> channelSetDataAdapter) : base(databaseProvider, ObjectNames.Log200, ObjectTypes.Uuid)
        {
            _channelSetDataAdapter = channelSetDataAdapter;
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
                var uidWellbore = parentUri.Value.ObjectId;
                query = query.Where(x => x.Wellbore.Uuid == uidWellbore);
            }

            return query
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(WitsmlQueryParser parser, Log dataObject)
        {
            // Add ChannelSets + data via the ChannelSet data adapter
            foreach (var childParser in parser.ForkProperties("ChannelSet", ObjectTypes.ChannelSet))
            {
                var channelSet = WitsmlParser.Parse<ChannelSet>(childParser.Context.Xml);
                _channelSetDataAdapter.Add(childParser, channelSet);
            }

            // Clear ChannelSet data properties
            foreach (var channelSet in dataObject.ChannelSet)
            {
                channelSet.Data = null;
            }

            return base.Add(parser, dataObject);
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Update(WitsmlQueryParser parser, Log dataObject)
        {
            // Update ChannelSets + data via the ChannelSet data adapter
            foreach (var childParser in parser.ForkProperties("ChannelSet", ObjectTypes.ChannelSet))
            {
                var channelSet = WitsmlParser.Parse<ChannelSet>(childParser.Context.Xml);
                _channelSetDataAdapter.Update(childParser, channelSet);
            }

            var uri = GetUri(dataObject);
            var ignored = new[] { "Data" };

            UpdateEntity(parser, uri, ignored);

            return new WitsmlResult(ErrorCodes.Success);
        }
    }
}
