using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Well" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML200.Well}" />
    [Export(typeof(IEtpDataAdapter<Well>))]
    [Export200(ObjectTypes.Well, typeof(IEtpDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well200DataAdapter : MongoDbDataAdapter<Well>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Well200DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Well200DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Well200, ObjectTypes.Uuid)
        {
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Well> GetAll(EtpUri? parentUri = null)
        {
            return GetQuery()
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public override WitsmlResult Put(Well entity)
        {
            var dataObjectId = entity.GetObjectId();

            if (!string.IsNullOrWhiteSpace(entity.Uuid) && Exists(dataObjectId))
            {
                entity.Citation = entity.Citation.Update();
                Logger.DebugFormat("Updating Well with Uuid '{0}' and citation title '{1}'.", entity.Uuid, entity.Citation.Title);

                Validate(Functions.PutObject, entity);
                Logger.DebugFormat("Validated Well with Uuid '{0}' and citation title '{1}'.", entity.Uuid, entity.Citation.Title);

                var parser = CreateQueryParser(Functions.PutObject, entity);

                UpdateEntity(parser, dataObjectId);
            }
            else
            {
                entity.Uuid = NewUid(entity.Uuid);
                entity.Citation = entity.Citation.Update(true);
                Logger.DebugFormat("Adding Well with Uuid '{0}' and citation title '{1}'.", entity.Uuid, entity.Citation.Title);

                Validate(Functions.PutObject, entity);
                Logger.DebugFormat("Validated Well with Uuid '{0}' and citation title '{1}'.", entity.Uuid, entity.Citation.Title);

                InsertEntity(entity);
            }

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
        }
    }
}
