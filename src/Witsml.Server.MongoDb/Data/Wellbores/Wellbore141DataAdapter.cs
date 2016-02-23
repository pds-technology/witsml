using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using log4net;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PDS.Framework;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Wellbore" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML141.Wellbore}" />
    /// <seealso cref="PDS.Witsml.Server.Data.IEtpDataAdapter{Energistics.DataAccess.WITSML141.Wellbore}" />
    /// <seealso cref="PDS.Witsml.Server.IWitsml141Configuration" />
    [Export(typeof(IWitsml141Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Wellbore>))]
    [Export(typeof(IEtpDataAdapter<Wellbore>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Wellbore141DataAdapter : MongoDbDataAdapter<Wellbore>, IEtpDataAdapter<Wellbore>, IWitsml141Configuration
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Wellbore141DataAdapter));
        private static readonly string ParentDbDocumentName = ObjectNames.Well141;
        private static readonly string DbDocumentName = ObjectNames.Wellbore141;
        private static readonly string Version141 = "1.4.1.1";

        /// <summary>
        /// Initializes a new instance of the <see cref="Wellbore141DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Wellbore141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider)
        {
        }

        [Import]
        public IContainer Container { get; set; }

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
                QueryEntities(parser, DbDocumentName, new List<string>() { "nameWell,NameWell", "name,Name" }));
        }

        /// <summary>
        /// Adds a WITSML wellbore object to data store.
        /// </summary>
        /// <param name="entity">WITSML wellbore object to be added</param>
        /// <returns>A WitsmlResult object that contains a return code and the UID of the new wellbore object if successful or an error message 
        public override WitsmlResult Add(Wellbore entity)
        {
            // Initialize the Uid if one has not been supplied.
            entity.Uid = NewUid(entity.Uid);

            ICollection<ValidationResult> validationResults;
            var validator = Container.Resolve<DataObjectValidator<Wellbore>>(new ObjectName("wellbore", Version141));
            validator.DataObject = entity;      
            var success = EntityValidator.TryValidate(validator, out validationResults);

            if (!success)           
                return new WitsmlResult((ErrorCodes)Enum.Parse(typeof(ErrorCodes), validationResults.First().ErrorMessage), validationResults.First().MemberNames.First());

            try
            {
                _log.DebugFormat("uidWell: {0}; uid: {1}", entity.UidWell, entity.Uid);

                CreateEntity(entity, DbDocumentName);
                var result = GetEntity(entity.Uid, DbDocumentName);
                if (result != null)
                {
                    return new WitsmlResult(ErrorCodes.Success, result.Uid);
                }
                else
                {
                    return new WitsmlResult(ErrorCodes.Unset, "Error adding wellbore");
                }
            }
            catch (Exception ex)
            {
                return new WitsmlResult(ErrorCodes.Unset, ex.Message + "\n" + ex.StackTrace);
            }
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
        public List<Wellbore> GetAll(string parentUri = null)
        {
            var database = DatabaseProvider.GetDatabase();
            var collection = database.GetCollection<Wellbore>(DbDocumentName);
            var uidWell = parentUri.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries).Last();

            return collection.AsQueryable()
                .Where(x => x.UidWell == uidWell)
                .OrderBy(x => x.Name)
                .ToList();
        }
    }
}
