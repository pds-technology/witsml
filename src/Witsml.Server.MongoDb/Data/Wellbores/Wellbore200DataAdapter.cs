using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Wellbore" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML200.Wellbore}" />
    [Export(typeof(IEtpDataAdapter<Wellbore>))]
    [Export200(ObjectTypes.Wellbore, typeof(IEtpDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Wellbore200DataAdapter : MongoDbDataAdapter<Wellbore>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Wellbore200DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Wellbore200DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Wellbore200, ObjectTypes.Uuid)
        {
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Wellbore> GetAll(EtpUri? parentUri = null)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                var uidWell = parentUri.Value.ObjectId;
                query = query.Where(x => x.ReferenceWell.Uuid == uidWell);
            }

            return query
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public override WitsmlResult Put(Wellbore entity)
        {
            var dataObjectId = entity.GetObjectId();

            if (!string.IsNullOrWhiteSpace(entity.Uuid) && Exists(dataObjectId))
            {
                entity.Citation = entity.Citation.Update();
                Logger.DebugFormat("Updating Wellbore with Uuid '{0}' and citation title '{1}'.", entity.Uuid, entity.Citation.Title);

                Validate(Functions.PutObject, entity);
                Logger.DebugFormat("Validated Wellbore with Uuid '{0}' and citation title '{1}'.", entity.Uuid, entity.Citation.Title);
                
                UpdateEntity(entity, dataObjectId);
            }
            else
            {
                entity.Uuid = NewUid(entity.Uuid);
                entity.Citation = entity.Citation.Update();
                Logger.DebugFormat("Adding Wellbore with Uuid '{0}' and citation title '{1}'.", entity.Uuid, entity.Citation.Title);

                Validate(Functions.PutObject, entity);
                Logger.DebugFormat("Validated Wellbore with Uuid '{0}' and citation title '{1}'.", entity.Uuid, entity.Citation.Title);

                InsertEntity(entity);
            }

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
        }
    }
}
