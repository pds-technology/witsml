## WITSML Server
The "PDS.Witsml.Web" solution builds PDS WITSML Server with MongoDB for data storage and configures Witsml.Web as an IIS web application. It contains the following projects: 

##### PDS.Witsml.Server.MongoDb
Contains the WitsmlDataAdapter implementation for MongoDB.
- MongoDbDataAdapter - is a data adapter that encapsulates CRUD functionality for WITSML objects.
````C#
    /// <summary>
    /// Updates a data object in the data store.
    /// </summary>
    /// <param name="parser">The input template parser.</param>
    /// <param name="dataObject">The data object to be updated.</param>
    public override void Update(WitsmlQueryParser parser, T dataObject)
    {
        var uri = GetUri(dataObject);
        using (var transaction = DatabaseProvider.BeginTransaction(uri))
        {
            UpdateEntity(parser, uri, transaction);
            ValidateUpdatedEntity(Functions.UpdateInStore, uri);
            transaction.Commit();
        }
    }
````
- MongoDbUtility - a utility class that encapsulates helper methods for parsing element in query and update
````C#
    /// <summary>
    /// Gets the list of URI by object type.
    /// </summary>
    /// <param name="uris">The URI list.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <returns>the list of URI specified by the object type.</returns>
    public static List<EtpUri> GetObjectUris(IEnumerable<EtpUri> uris, string objectType)
    {
        return uris.Where(u => u.ObjectType == objectType).ToList();
    }
````
- MongoTransaction - encapsulates transaction-like behavior for MongoDB
````C#
    /// <summary>
    /// Commits the transaction in MongoDb.
    /// </summary>
    public void Commit()
    {
        var database = DatabaseProvider.GetDatabase();
        foreach (var transaction in Transactions.Where(t => t.Status == TransactionStatus.Pending && t.Action == MongoDbAction.Delete))
        {
            Delete(database, transaction);
        }

        ClearTransactions();
        Committed = true;
    }
````
##### PDS.Witsml.Server.MongoDb.IntegrationTest
Integration tests for PDS.Witsml.Server.MongoDb.
<br>
<br>
##### PDS.Witsml.Web
Configures and hosts PDS WITSML Server on IIS.

---

### Copyright and License
Copyright &copy; 2016 Petrotechnical Data Systems

Released under the Apache License, Version 2.0