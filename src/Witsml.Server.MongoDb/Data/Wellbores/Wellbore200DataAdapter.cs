using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Wellbore" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML200.Wellbore}" />
    [Export(typeof(IEtpDataAdapter<Wellbore>))]
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
        public override List<Wellbore> GetAll(string parentUri = null)
        {
            var uidWell = parentUri.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries).Last();
            var query = GetQuery().AsQueryable();

            if (!string.IsNullOrWhiteSpace(uidWell))
            {
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
            if (!string.IsNullOrWhiteSpace(entity.Uuid) && Exists(entity.Uuid))
            {
                throw new NotImplementedException();
            }
            else
            {
                entity.Uuid = NewUid(entity.Uuid);
                entity.Citation = entity.Citation.Update();

                var validator = Container.Resolve<IDataObjectValidator<Wellbore>>();
                validator.Validate(Functions.PutObject, entity);

                InsertEntity(entity);
            }

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
        }
    }
}
