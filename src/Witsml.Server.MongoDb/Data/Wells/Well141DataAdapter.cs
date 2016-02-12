using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using MongoDB.Driver;

namespace PDS.Witsml.Server.Data.Wells
{
    [Export(typeof(IWitsml141Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Well>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Well141DataAdapter : MongoDbDataAdapter<Well>, IWitsml141Configuration
    {
        private static readonly string DbDocumentName = ObjectNames.Well141;

        [ImportingConstructor]
        public Well141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider)
        {
        }

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

        public override WitsmlResult Add(Well entity)
        {
            var validationResults = new Dictionary<ErrorCodes, string>();

            // Initialize the Uid if one has not been supplied.
            entity.Uid = NewUid(entity.Uid);

            // TODO: Move existing well validation to a central place.
            //Validate(entity, validationResults);

            if (validationResults.Keys.Any())
            {
                return new WitsmlResult(validationResults.Keys.First(), validationResults.Values.First());
            }

            try
            {
                CreateEntity(entity, DbDocumentName);
                var result = GetEntity(entity.Uid, DbDocumentName);
                if (result != null)
                {
                    return new WitsmlResult(ErrorCodes.Success, result.Uid);
                }
                else
                {
                    return new WitsmlResult(ErrorCodes.Unset, "Error adding well");
                }
            }
            catch (Exception ex)
            {
                return new WitsmlResult(ErrorCodes.Unset, ex.Message + "\n" + ex.StackTrace);
            }
        }

        public override WitsmlResult Update(Well entity)
        {
            throw new NotImplementedException();
        }

        public override WitsmlResult Delete(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }
    }
}
