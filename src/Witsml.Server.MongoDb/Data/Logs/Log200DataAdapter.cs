using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Datatypes;
using MongoDB.Driver;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML200.Log}" />
    [Export(typeof(IEtpDataAdapter<Log>))]
    [Export200(ObjectTypes.Log, typeof(IEtpDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log200DataAdapter : MongoDbDataAdapter<Log>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Log200DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Log200DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Log200, ObjectTypes.Uuid)
        {
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
        /// <param name="entity">The entity.</param>
        public override WitsmlResult Put(Log entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.Uuid) && Exists(entity.GetObjectId()))
            {
                throw new NotImplementedException();
            }
            else
            {
                entity.Uuid = NewUid(entity.Uuid);
                entity.Citation = entity.Citation.Update();

                var validator = Container.Resolve<IDataObjectValidator<Log>>();
                validator.Validate(Functions.PutObject, entity);

                var channelData = new Dictionary<string, string>();
                var indicesMap = new Dictionary<string, List<ChannelIndexInfo>>();

                SaveChannelSets(entity, channelData, indicesMap);
                InsertEntity(entity);

                var channelDataAdapter = new ChannelDataAdapter(DatabaseProvider);               
                channelDataAdapter.WriteChannelSetValues(entity.Uuid, channelData, indicesMap);
            }

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
        }

        /// <summary>
        /// Saves the channel sets to its own collection in addition as a property for the log.
        /// </summary>
        /// <param name="entity">The log entity.</param>
        /// <param name="channelData">The collection to extract the channel set data.</param>
        /// <param name="indicesMap">The indices map for the list of channel set.</param>
        private void SaveChannelSets(Log entity, Dictionary<string, string> channelData, Dictionary<string, List<ChannelIndexInfo>> indicesMap)
        {
            var collection = GetCollection<ChannelSet>(ObjectNames.ChannelSet200);

            collection.BulkWrite(entity.ChannelSet
                .Select(cs =>
                {
                    if (cs.Data != null && !string.IsNullOrEmpty(cs.Data.Data))
                    {
                        var uuid = cs.Uuid;
                        channelData.Add(uuid, cs.Data.Data);
                        indicesMap.Add(uuid, CreateChannelSetIndexInfo(cs.Index));
                        cs.Data.Data = null;
                    }
                    return (WriteModel<ChannelSet>)new InsertOneModel<ChannelSet>(cs);
                }));
        }

        /// <summary>
        /// Creates the list of index info to be used for channel set values.
        /// </summary>
        /// <param name="indices">The original index list of a channel set.</param>
        /// <returns>The list of index info.</returns>
        private List<ChannelIndexInfo> CreateChannelSetIndexInfo(List<ChannelIndex> indices)
        {
            var indicesInfo = new List<ChannelIndexInfo>();
            foreach (var index in indices)
            {
                var indexInfo = new ChannelIndexInfo
                {
                    Mnemonic = index.Mnemonic,
                    Increasing = index.Direction == IndexDirection.increasing,
                    IsTimeIndex = index.IndexType == ChannelIndexType.datetime || index.IndexType == ChannelIndexType.elapsedtime
                };
                indicesInfo.Add(indexInfo);
            }

            return indicesInfo;
        }
    }
}
