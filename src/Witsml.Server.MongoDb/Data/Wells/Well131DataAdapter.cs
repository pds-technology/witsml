using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Well" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML131.Well}" />
    /// <seealso cref="PDS.Witsml.Server.Configuration.IWitsml131Configuration" />
    [Export(typeof(IWitsml131Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Well>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MongoDbWellDataAdapter : MongoDbDataAdapter<Well>, IWitsml131Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbWellDataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public MongoDbWellDataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Well131)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Well"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            capServer.Add(Functions.GetFromStore, ObjectTypes.Well);
            capServer.Add(Functions.AddToStore, ObjectTypes.Well);
            //capServer.Add(Functions.UpdateInStore, ObjectTypes.Well);
            //capServer.Add(Functions.DeleteFromStore, ObjectTypes.Well);
        }

        /// <summary>
        /// Queries the object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>
        /// Queried objects.
        /// </returns>
        public override WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser)
        {
            List<string> fields = null;
            if (parser.ReturnElements() == OptionsIn.ReturnElements.IdOnly.Value)
                fields = new List<string> { IdPropertyName, "name" };

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                new WellList()
                {
                    Well = QueryEntities<WellList>(parser, fields)
                });
        }

        /// <summary>
        /// Adds a <see cref="Well"/> to the data store.
        /// </summary>
        /// <param name="entity">The object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(Well entity)
        {
            entity.Uid = NewUid(entity.Uid);
            entity.CommonData = entity.CommonData.Update();

            InsertEntity(entity);

            return new WitsmlResult(ErrorCodes.Success, entity.Uid);
        }
    }
}
