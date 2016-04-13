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
    [Export(typeof(IEtpDataAdapter))]
    [Export(typeof(IEtpDataAdapter<Log>))]
    [Export200(ObjectTypes.Log, typeof(IEtpDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log200DataAdapter : MongoDbDataAdapter<Log>
    {
        private readonly IEtpDataAdapter<ChannelSet> _channelSetDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log200DataAdapter" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="channelSetDataAdapter">The channel set data adapter.</param>
        [ImportingConstructor]
        public Log200DataAdapter(IDatabaseProvider databaseProvider, IEtpDataAdapter<ChannelSet> channelSetDataAdapter) : base(databaseProvider, ObjectNames.Log200, ObjectTypes.Uuid)
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
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="parser">The input parser.</param>
        /// <returns>A WITSML result.</returns>
        public override WitsmlResult Put(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<Log>();

            Logger.DebugFormat("Putting Log with uid '{0}'.", uri.ObjectId);

            // save ChannelSets + data via the ChannelSet data adapter
            foreach (var childParser in parser.ForkProperties("ChannelSet", ObjectTypes.ChannelSet))
            {
                _channelSetDataAdapter.Put(childParser);
            }

            if (!string.IsNullOrWhiteSpace(uri.ObjectId) && Exists(uri))
            {
                //Validate(Functions.UpdateInStore, entity);
                //Logger.DebugFormat("Validated Log with uid '{0}' for Update", uri.ObjectId);

                var ignored = new[] { "Data" };
                UpdateEntity(parser, uri, ignored);

                return new WitsmlResult(ErrorCodes.Success);
            }
            else
            {
                var entity = Parse(parser.Context.Xml);

                // Clear ChannelSet data properties
                foreach (var channelSet in entity.ChannelSet)
                {
                    channelSet.Data = null;
                }

                entity.Uuid = NewUid(entity.Uuid);
                entity.Citation = entity.Citation.Create();
                Logger.DebugFormat("Adding Log with uid '{0}' and name '{1}'", entity.Uuid, entity.Citation.Title);

                Validate(Functions.AddToStore, entity);
                InsertEntity(entity);

                return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
            }
        }
    }
}
