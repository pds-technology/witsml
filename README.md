[![Build status](https://ci.appveyor.com/api/projects/status/iej6hbx4o93kg6ss?svg=true)](https://ci.appveyor.com/project/PDS/witsml-server)
[![Coverity Scan Build Status](https://scan.coverity.com/projects/18119/badge.svg)](https://scan.coverity.com/projects/pds-technology-witsml-server)

## PDS WITSMLstudio Store

**Quick Links:**&nbsp;
[Blog](https://witsml.pds.technology/blog) |
[Getting Started](https://witsml.pds.technology/docs/getting-started) |
[Documentation](https://witsml.pds.technology/docs/documentation) |
[Downloads](https://witsml.pds.technology/docs/downloads) |
[Support](https://witsml.pds.technology/docs/support)

> **Note:** Be sure to perform a recursive clone of the repository to retrieve the `witsml` submodule.

The "PDS.WITSMLstudio.Store" solution builds PDS WITSMLstudio Store with MongoDB for data storage and configures Store as an IIS web application. It contains the following projects: 

##### Store
Configures and hosts PDS WITSMLstudio Store on IIS.

##### Store.MongoDb
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

##### Store.MongoDb.IntegrationTest
Integration tests for Store.MongoDb.

---

### Copyright and License
Copyright &copy; 2018 PDS Americas LLC

Released under the PDS Open Source WITSML™ Product License Agreement
http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement

---

### Export Compliance

This source code makes use of cryptographic software:
- SSL/TLS is optionally used to secure web communications

The country in which you currently reside may have restrictions on the import, possession,
use, and/or re-export to another country, of encryption software.  BEFORE using any
encryption software, please check your country's laws, regulations and policies concerning
the import, possession, or use, and re-export of encryption software, to see if this is
permitted.  See <http://www.wassenaar.org/> for more information.

The U.S. Government Department of Commerce, Bureau of Industry and Security (BIS), has
classified this source code as Export Control Classification Number (ECCN) 5D002.c.1, which
includes information security software using or performing cryptographic functions with
symmetric and/or asymmetric algorithms.

This source code is published here:
> https://github.com/pds-technology/witsml-server

In accordance with US Export Administration Regulations (EAR) Section 742.15(b), this
source code is not subject to EAR:
 - This source code has been made publicly available in accordance with EAR Section
   734.3(b)(3)(i) by publishing it in accordance with EAR Section 734.7(a)(4) at the above
   URL.
 - The BIS and the ENC Encryption Request Coordinator have been notified via e-mail of this
   URL.