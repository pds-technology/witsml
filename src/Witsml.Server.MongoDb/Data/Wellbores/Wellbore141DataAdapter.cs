using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using log4net;
using PDS.Framework;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Wellbore" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML141.Wellbore}" />
    /// <seealso cref="PDS.Witsml.Server.IWitsml141Configuration" />
    [Export(typeof(IWitsml141Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Wellbore>))]
    [Export(typeof(IEtpDataAdapter<Wellbore>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Wellbore141DataAdapter : MongoDbDataAdapter<Wellbore>, IWitsml141Configuration
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Wellbore141DataAdapter));

        /// <summary>
        /// Initializes a new instance of the <see cref="Wellbore141DataAdapter" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Wellbore141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Wellbore141)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Wellbore"/> object.
        /// </summary>
        /// <param name="capServer">The capServer object.</param>
        public void GetCapabilities(CapServer capServer)
        {
            capServer.Add(Functions.GetFromStore, ObjectTypes.Wellbore);
            capServer.Add(Functions.AddToStore, ObjectTypes.Wellbore);
            //capServer.Add(Functions.UpdateInStore, ObjectTypes.Wellbore);
            //capServer.Add(Functions.DeleteFromStore, ObjectTypes.Wellbore);
        }

        public override WitsmlResult<List<Wellbore>> Query(WitsmlQueryParser parser)
        {
            return new WitsmlResult<List<Wellbore>>(
                ErrorCodes.Success,
                QueryEntities(parser, new List<string>() { "nameWell,NameWell", "name,Name" }));
        }

        /// <summary>
        /// Adds a WITSML wellbore object to data store.
        /// </summary>
        /// <param name="entity">WITSML wellbore object to be added</param>
        /// <returns>A WitsmlResult object that contains a return code and the UID of the new wellbore object if successful or an error message 
        public override WitsmlResult Add(Wellbore entity)
        {
            entity.Uid = NewUid(entity.Uid);
            entity.CommonData = entity.CommonData.Update();

            var validator = Container.Resolve<IDataObjectValidator<Wellbore>>();
            validator.Validate(Functions.AddToStore, entity);

            _log.DebugFormat("Add new wellbore with uidWell: {0}; uid: {1}", entity.UidWell, entity.Uid);
            InsertEntity(entity, DbCollectionName);

            return new WitsmlResult(ErrorCodes.Success, entity.Uid);
        }

        public override WitsmlResult Delete(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }

        public override WitsmlResult Update(Wellbore entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Wellbore> GetAll(string parentUri = null)
        {
            var uidWell = parentUri.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries).Last();

            return GetQuery()
                .Where(x => x.UidWell == uidWell)
                .OrderBy(x => x.Name)
                .ToList();
        }
    }
}
