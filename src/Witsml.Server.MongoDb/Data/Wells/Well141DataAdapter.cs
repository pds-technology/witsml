using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML141;
using log4net;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Well" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML141.Well}" />
    /// <seealso cref="PDS.Witsml.Server.Data.IEtpDataAdapter{Energistics.DataAccess.WITSML141.Well}" />
    /// <seealso cref="PDS.Witsml.Server.IWitsml141Configuration" />
    [Export(typeof(IWitsml141Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Well>))]
    [Export(typeof(IEtpDataAdapter<Well>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well141DataAdapter : MongoDbDataAdapter<Well>, IEtpDataAdapter<Well>, IWitsml141Configuration
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Well141DataAdapter));
        private static readonly string DbDocumentName = ObjectNames.Well141;

        /// <summary>
        /// Initializes a new instance of the <see cref="Well141DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Well141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider)
        {
        }

        /// <summary>
        /// Gets the supported <see cref="Well"/> functionalities for the capServer object.
        /// </summary>
        /// <param name="capServer">The capServer object.</param>
        public void GetCapabilities(CapServer capServer)
        {
            capServer.Add(Functions.GetFromStore, ObjectTypes.Well);
            capServer.Add(Functions.AddToStore, ObjectTypes.Well);
            //capServer.Add(Functions.UpdateInStore, ObjectTypes.Well);
            //capServer.Add(Functions.DeleteFromStore, ObjectTypes.Well);
        }

        public override WitsmlResult<List<Well>> Query(WitsmlQueryParser parser)
        {
            return new WitsmlResult<List<Well>>(
                ErrorCodes.Success,
                QueryEntities(parser, DbDocumentName, new List<string>() { "name,Name" }));
        }

        /// <summary>
        /// Adds a <see cref="Well"/> to the data store.
        /// </summary>
        /// <param name="entity">The <see cref="Well"/> to be added.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(Well entity)
        {
            var validationResults = new Dictionary<ErrorCodes, string>();

            // Initialize the Uid if one has not been supplied.
            entity.Uid = NewUid(entity.Uid);
            entity.CommonData = UpdateLastChangeTime(entity.CommonData);

            // TODO: Move existing well validation to a central place.
            //Validate(entity, validationResults);

            //if (validationResults.Keys.Any())
            //{
            //    return new WitsmlResult(validationResults.Keys.First(), validationResults.Values.First());
            //}

            _log.DebugFormat("Add new well with uid: {0}", entity.Uid);
            CreateEntity(entity, DbDocumentName);

            return new WitsmlResult(ErrorCodes.Success, entity.Uid);
        }

        public override WitsmlResult Update(Well entity)
        {
            throw new NotImplementedException();
        }

        public override WitsmlResult Delete(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public List<Well> GetAll(string parentUri = null)
        {
            var database = DatabaseProvider.GetDatabase();
            var collection = database.GetCollection<Well>(DbDocumentName);

            return collection.AsQueryable()
                .OrderBy(x => x.Name)
                .ToList();
        }
    }
}
