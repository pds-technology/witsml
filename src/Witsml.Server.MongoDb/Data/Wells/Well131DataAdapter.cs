using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.Datatypes;
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
    [Export(typeof(IEtpDataAdapter<Well>))]
    [Export131(ObjectTypes.Well, typeof(IEtpDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well131DataAdapter : MongoDbDataAdapter<Well>, IWitsml131Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Well131DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Well131DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Well131)
        {
            Logger.Debug("Instance created.");
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Well"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            Logger.DebugFormat("Getting capabilities for server '{0}'.", capServer.Name);

            capServer.Add(Functions.GetFromStore, ObjectTypes.Well);
            capServer.Add(Functions.AddToStore, ObjectTypes.Well);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Well);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Well);
        }

        /// <summary>
        /// Queries the object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>Queried objects.</returns>
        public override WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser)
        {
            var returnElements = parser.ReturnElements();
            Logger.DebugFormat("Querying with return elements '{0}'", returnElements);

            var fields = (OptionsIn.ReturnElements.IdOnly.Equals(returnElements))
                ? new List<string> { IdPropertyName, NamePropertyName }
                : null;

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                new WellList()
                {
                    Well = QueryEntities(parser, fields)
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
            Logger.DebugFormat("Adding Well with uid '{0}' and name '{1}'.", entity.Uid, entity.Name);

            entity.Uid = NewUid(entity.Uid);
            entity.CommonData = entity.CommonData.Update();
            Validate(Functions.AddToStore, entity);

            Logger.DebugFormat("Well with uid '{0}' and name {1} validated for Add", entity.Uid, entity.Name);
            InsertEntity(entity);

            return new WitsmlResult(ErrorCodes.Success, entity.Uid);
        }

        /// <summary>
        /// Updates the specified object.
        /// </summary>
        /// <param name="entity">The object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Update(Well entity)
        {
            Logger.DebugFormat("Updating Well with uid '{0}' and name '{1}'.", entity.Uid, entity.Name);

            entity.CommonData = entity.CommonData.Update();
            Validate(Functions.UpdateInStore, entity);

            Logger.DebugFormat("Validated Well with uid '{0}' and name {1} for Update", entity.Uid, entity.Name);
            UpdateEntity(entity, entity.GetObjectId());

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The parser that specifies the object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Delete(WitsmlQueryParser parser)
        {
            var entity = Parse(parser.Context.Xml);
            var dataObjectId = entity.GetObjectId();

            DeleteEntity(dataObjectId);

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Well> GetAll(EtpUri? parentUri = null)
        {
            Logger.Debug("Fetching all Wells.");

            return GetQuery()
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public override WitsmlResult Put(Well entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.Uid) && Exists(entity.GetObjectId()))
            {
                Logger.DebugFormat("Updating Well with uid '{0}' and name '{1}'.", entity.Uid, entity.Name);
                return Update(entity);
            }
            else
            {
                Logger.DebugFormat("Adding Well with uid '{0}' and name '{1}'.", entity.Uid, entity.Name);
                return Add(entity);
            }
        }

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>An instance of <see cref="Well" />.</returns>
        protected override Well Parse(string xml)
        {
            var list = WitsmlParser.Parse<WellList>(xml);
            return list.Well.FirstOrDefault();
        }
    }
}
