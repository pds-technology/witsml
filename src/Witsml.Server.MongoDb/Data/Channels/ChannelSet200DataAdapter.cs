using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="ChannelSet" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML200.ChannelSet}" />
    [Export(typeof(IEtpDataAdapter<ChannelSet>))]
    [Export200(ObjectTypes.ChannelSet, typeof(IEtpDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChannelSet200DataAdapter : MongoDbDataAdapter<ChannelSet>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSet200DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public ChannelSet200DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.ChannelSet200, ObjectTypes.Uuid)
        {
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<ChannelSet> GetAll(EtpUri? parentUri = null)
        {
            var query = GetQuery().AsQueryable();

            //if (parentUri != null)
            //{
            //    var uidLog = parentUri.Value.ObjectId;
            //    query = query.Where(x => x.Log.Uuid == uidLog);
            //}

            return query
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public override WitsmlResult Put(ChannelSet entity)
        {
            var dataObjectId = entity.GetObjectId();

            if (!string.IsNullOrWhiteSpace(entity.Uuid) && Exists(dataObjectId))
            {
                entity.Citation = entity.Citation.Update();

                Validate(Functions.PutObject, entity);

                // Get Reader 

                // Clear Data

                UpdateEntity(entity, dataObjectId);

                // Merge ChannelDataValues
            }
            else
            {
                entity.Uuid = NewUid(entity.Uuid);
                entity.Citation = entity.Citation.Update();

                Validate(Functions.PutObject, entity);

                // Get Reader 
                var reader = ChannelDataExtensions.GetReader(entity);
                
                // Clear Data

                InsertEntity(entity);

                // Save ChannelDataValues
            }

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
        }
    }
}
