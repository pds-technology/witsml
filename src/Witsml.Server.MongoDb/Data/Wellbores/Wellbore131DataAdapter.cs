using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Wellbores
{
    [Export(typeof(IWitsml131Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Wellbore>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Wellbore131DataAdapter : MongoDbDataAdapter<Wellbore>, IWitsml131Configuration
    {
        private static readonly string DbDocumentName = ObjectNames.Wellbore131;

        [ImportingConstructor]
        public Wellbore131DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider)
        {
        }

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

        public override WitsmlResult Add(Wellbore entity)
        {
            var validationResults = new Dictionary<ErrorCodes, string>();

            // Initialize the Uid if one has not been supplied.
            entity.Uid = NewUid(entity.Uid);

            // TODO: Move existing wellbore validation to a central place.
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
    }
}
