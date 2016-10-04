## WITSML Server

**Quick Links:**&nbsp;
[Blog](https://witsml.pds.technology/blog) |
[Getting Started](https://witsml.pds.technology/docs/getting-started) |
[Documentation](https://witsml.pds.technology/docs/documentation) |
[Downloads](https://witsml.pds.technology/docs/downloads) |
[Support](https://witsml.pds.technology/docs/support)

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

##### PDS.Witsml.Web
Configures and hosts PDS WITSML Server on IIS.

---

### Copyright and License
Copyright &copy; 2016 Petrotechnical Data Systems

Released under the Apache License, Version 2.0

---

### Cryptographic Notice

This source code makes use of cryptographic software.

The country in which you currently reside may have restrictions on the import, possession, use, and/or re-export to another country, of encryption software.  BEFORE using any encryption software, please check your country's laws, regulations and policies concerning the import, possession, or use, and re-export of encryption software, to see if this is permitted.  See <http://www.wassenaar.org/> for more information.

The U.S. Government Department of Commerce, Bureau of Industry and Security (BIS), has classified this source code as Export Commodity Control Number (ECCN) 5D002.c.1, which includes information security software using or performing cryptographic functions with symmetric and/or asymmetric algorithms.

This source code is published here:  

> <https://github.com/pds-technology/>

In accordance with BIS Export Administration Regulations (EAR) Section 742.15(b), this source code is exempt from EAR:

- This source code has been made publicly available in accordance with BIS EAR Section 734.3(b)(3) by publishing it in accordance with BIS EAR Section 734.7(a)(4) at the above URL.
- The BIS and the ENC Encryption Request Coordinator have been notified via e-mail of this URL.

The following provides more details on the included cryptographic software:

- SSL/TLS is optionally used to secure web communications
