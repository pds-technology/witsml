using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Wellbores
{
    [Export(typeof(IWitsml131Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Wellbore>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Wellbore131DataAdapter : MongoDbDataAdapter<Wellbore>, IWitsml131Configuration
    {
        [ImportingConstructor]
        public Wellbore131DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Wellbore131)
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
                QueryEntities(parser, new List<string>() { "nameWell,NameWell", "name,Name" }));
        }

        public override WitsmlResult Add(Wellbore entity)
        {
            entity.Uid = NewUid(entity.Uid);

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
    }
}
