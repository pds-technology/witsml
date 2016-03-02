using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using log4net;
using PDS.Witsml.Server.Configuration;
using System.Reflection;
using Energistics.DataAccess;

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Well" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML141.Well}" />
    /// <seealso cref="PDS.Witsml.Server.Configuration.IWitsml141Configuration" />
    [Export(typeof(IWitsml141Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Well>))]
    [Export(typeof(IEtpDataAdapter<Well>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well141DataAdapter : MongoDbDataAdapter<Well>, IWitsml141Configuration
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Well141DataAdapter));

        /// <summary>
        /// Initializes a new instance of the <see cref="Well141DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Well141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Well141)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Well"/> object.
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
            if (parser.RequestObjectSelectionCapability() == OptionsIn.RequestObjectSelectionCapability.True.Value)
            {
                PropertyInfo[] propertyInfo = typeof(Well).GetProperties();

                Well well = QueryFirstEntity();
                if (well == null)
                    well = new Well();

                foreach (PropertyInfo property in propertyInfo)
                {
                    object value = property.GetValue(well);
                    if (value == null && property.PropertyType==typeof(string))
                        property.SetValue(well, " "); // TODO: To handle null property values.
                }

                return new WitsmlResult<List<Well>>(
                    ErrorCodes.Success,
                    new List<Well>() { well });
            }

            var wellList = EnergisticsConverter.XmlToObject<WellList>(parser.Context.Xml);

            return new WitsmlResult<List<Well>>(
                ErrorCodes.Success,
                QueryEntities(parser, wellList.Well));
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
            entity.Uid = NewUid(entity.Uid);
            entity.CommonData = entity.CommonData.Update();

            var validator = Container.Resolve<IDataObjectValidator<Well>>();
            validator.Validate(Functions.AddToStore, entity);

            _log.DebugFormat("Add new well with uid: {0}", entity.Uid);
            InsertEntity(entity);

            return new WitsmlResult(ErrorCodes.Success, entity.Uid);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Well> GetAll(string parentUri = null)
        {
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
            if (!string.IsNullOrWhiteSpace(entity.Uid) && Exists(entity.Uid))
            {
                return Update(entity);
            }
            else
            {
                return Add(entity);
            }
        }
    }
}
