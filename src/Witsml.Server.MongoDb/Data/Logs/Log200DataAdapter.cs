using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using MongoDB.Driver;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML200.Log}" />
    [Export(typeof(IEtpDataAdapter<Log>))]
    [Export200(ObjectTypes.Log, typeof(IEtpDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log200DataAdapter : MongoDbDataAdapter<Log>
    {
        private readonly IEtpDataAdapter<ChannelSet> _channelSetDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log200DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Log200DataAdapter(IDatabaseProvider databaseProvider, IEtpDataAdapter<ChannelSet> channelSetDataAdapter) : base(databaseProvider, ObjectNames.Log200, ObjectTypes.Uuid)
        {
            _channelSetDataAdapter = channelSetDataAdapter;
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Log> GetAll(EtpUri? parentUri = null)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                var uidWellbore = parentUri.Value.ObjectId;
                query = query.Where(x => x.Wellbore.Uuid == uidWellbore);
            }

            return query
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="parser">The input parser.</param>
        /// <returns>A WITSML result.</returns>
        public override WitsmlResult Put(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<Log>();

            Logger.DebugFormat("Putting Log with uid '{0}'.", uri.ObjectId);

            // save ChannelSets + data via the ChannelSet data adapter
            foreach (var childParser in parser.ForkProperties("ChannelSet", ObjectTypes.ChannelSet))
            {
                _channelSetDataAdapter.Put(childParser);
            }

            if (!string.IsNullOrWhiteSpace(uri.ObjectId) && Exists(uri))
            {
                //Validate(Functions.UpdateInStore, entity);
                //Logger.DebugFormat("Validated Log with uid '{0}' for Update", uri.ObjectId);

                var ignored = new[] { "Data" };
                UpdateEntity(parser, uri, ignored);

                return new WitsmlResult(ErrorCodes.Success);
            }
            else
            {
                var entity = Parse(parser.Context.Xml);

                // Clear ChannelSet data properties
                foreach (var channelSet in entity.ChannelSet)
                {
                    channelSet.Data = null;
                }

                entity.Uuid = NewUid(entity.Uuid);
                entity.Citation = entity.Citation.Create();
                Logger.DebugFormat("Adding Log with uid '{0}' and name '{1}'", entity.Uuid, entity.Citation.Title);

                Validate(Functions.AddToStore, entity);
                InsertEntity(entity);

                return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
            }
        }
    }
}
