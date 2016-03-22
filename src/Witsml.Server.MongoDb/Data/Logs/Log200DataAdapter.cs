using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using MongoDB.Driver;
using PDS.Witsml.Server.Data.Wellbores;
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
        public Log200DataAdapter(IDatabaseProvider databaseProvider, ChannelDataAdapter channelDataAdapter) : base(databaseProvider, ObjectNames.Log200, ObjectTypes.Uuid)
        {
            ChannelDataAdapter = channelDataAdapter;
        }

        public ChannelDataAdapter ChannelDataAdapter { get; set; }

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
            var dataObjectId = entity.GetObjectId();

            if (!string.IsNullOrWhiteSpace(entity.Uuid) && Exists(dataObjectId))
            {
                entity.Citation = entity.Citation.Update();

                Validate(Functions.PutObject, entity);
                UpdateEntity(entity, dataObjectId);

                // TODO: update channel data values
            }
            else
            {
                entity.Uuid = NewUid(entity.Uuid);
                entity.Citation = entity.Citation.Update();

                Validate(Functions.PutObject, entity);

                var channelData = new Dictionary<string, string>();
                var indicesMap = new Dictionary<string, List<ChannelIndexInfo>>();

                ChannelDataAdapter.SaveChannelSets(entity, channelData, indicesMap);
                InsertEntity(entity);
                ChannelDataAdapter.WriteChannelSetValues(entity.Uuid, channelData, indicesMap);
            }

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
        }
    }
}
