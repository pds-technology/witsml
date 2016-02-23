using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Wells
{
    [Export(typeof(IWitsml131Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Well>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MongoDbWellDataAdapter : MongoDbDataAdapter<Well>, IWitsml131Configuration
    {
        [ImportingConstructor]
        public MongoDbWellDataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Well131)
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
                QueryEntities(parser, new List<string>() { "name,Name" }));
        }

        public override WitsmlResult Add(Well entity)
        {
            entity.Uid = NewUid(entity.Uid);

            InsertEntity(entity, DbCollectionName);

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
    }
}
