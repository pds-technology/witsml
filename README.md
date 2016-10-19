## WITSML

**Quick Links:**&nbsp;
[Blog](https://witsml.pds.technology/blog) |
[Getting Started](https://witsml.pds.technology/docs/getting-started) |
[Documentation](https://witsml.pds.technology/docs/documentation) |
[Downloads](https://witsml.pds.technology/docs/downloads) |
[Support](https://witsml.pds.technology/docs/support)

The “PDS.Witsml” solution provides reusable components referenced by all PDS WITSML applications containing the following projects: 

##### PDS.Framework
Provides the composition container used to resolve dependencies.

##### PDS.Framework.Web
Configures the composition container to resolve dependencies for web projects and provides security.

##### PDS.Witsml
Contains common classes related to WITSML that are referenced by other projects, including but not limited to the following:

- ChannelDataReader - facilitates parsing and reading of log channel data
````C#
    /// <summary>
    /// Gets multiple readers for each LogData from a <see cref="Witsml141.Log"/> instance.
    /// </summary>
    /// <param name="log">The log.</param>
    /// <returns>An <see cref="IEnumerable{ChannelDataReader}"/>.</returns>
    public static IEnumerable<ChannelDataReader> GetReaders(this Witsml141.Log log)
    {
        if (log?.LogData == null) yield break;

        _log.DebugFormat("Creating ChannelDataReaders for {0}", log.GetType().FullName);

        var isTimeIndex = log.IsTimeLog();
        var increasing = log.IsIncreasing();

        foreach (var logData in log.LogData)
        {
            if (logData?.Data == null || !logData.Data.Any())
                continue;

            var mnemonics = ChannelDataReader.Split(logData.MnemonicList);
            var units = ChannelDataReader.Split(logData.UnitList);
            var nullValues = log.GetNullValues(mnemonics).Skip(1).ToArray();

            // Split index curve from other value curves
            var indexCurve = log.LogCurveInfo.GetByMnemonic(log.IndexCurve) ?? new Witsml141.ComponentSchemas.LogCurveInfo
            {
                Mnemonic = new Witsml141.ComponentSchemas.ShortNameStruct(mnemonics.FirstOrDefault()),
                Unit = units.FirstOrDefault()
            };

            // Skip index curve when passing mnemonics to reader
            mnemonics = mnemonics.Skip(1).ToArray();
            units = units.Skip(1).ToArray();

            yield return new ChannelDataReader(logData.Data, mnemonics.Length + 1, mnemonics, units, nullValues, log.GetUri(), dataDelimiter: log.GetDataDelimiterOrDefault())
                // Add index curve to separate collection
                .WithIndex(indexCurve.Mnemonic.Value, indexCurve.Unit, increasing, isTimeIndex);
        }
    }
````
- DataObjectNavigator - a framework for navigating a WITSML document
````C#
    /// <summary>
    /// Navigates the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="type">The type.</param>
    /// <param name="parentPath">The parent path.</param>
    protected void NavigateElement(XElement element, Type type, string parentPath = null)
    {
        if (IsIgnored(element.Name.LocalName)) return;

        var properties = GetPropertyInfo(type);
        var groupings = element.Elements().GroupBy(e => e.Name.LocalName);

        foreach (var group in groupings)
        {
            if (IsIgnored(group.Key, GetPropertyPath(parentPath, group.Key))) continue;

            var propertyInfo = GetPropertyInfoForAnElement(properties, group.Key);

            if (propertyInfo != null)
            {
                NavigateElementGroup(propertyInfo, group, parentPath);
            } 
            else
            {
                HandleInvalidElementGroup(group.Key);
            }
        }

        NavigateAttributes(element, parentPath, properties);
    }
````
- DataObjectValidator - a framework for validating a WITSML document
````C#
    /// <summary>
    /// Determines whether the specified object is valid.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection that holds failed-validation information.</returns>
    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
    {
        switch (Context.Function)
        {
            case Functions.GetFromStore:
                foreach (var result in ValidateForGet())
                    yield return result;
                break;

            case Functions.PutObject:
            case Functions.AddToStore:
                foreach (var result in ValidateProperties().Union(ValidateForInsert()))
                    yield return result;
                break;

            case Functions.UpdateInStore:
                foreach (var result in ValidateForUpdate())
                    yield return result;
                break;

            case Functions.DeleteObject:
            case Functions.DeleteFromStore:
                foreach (var result in ValidateForDelete())
                    yield return result;
                break;
        }
    }
````
- WitsmlParser - static helper methods to parse WITSML XML strings
````C#
    /// <summary>
    /// Parses the specified XML document using LINQ to XML.
    /// </summary>
    /// <param name="xml">The XML string.</param>
    /// <param name="debug">if set to <c>true</c> includes debug log output.</param>
    /// <returns>An <see cref="XDocument" /> instance.</returns>
    /// <exception cref="WitsmlException"></exception>
    public static XDocument Parse(string xml, bool debug = true)
    {
        if (debug)
        {
            _log.Debug("Parsing XML string.");
        }

        try
        {
            // remove invalid character along with leading/trailing white space
            xml = xml?.Trim().Replace("\x00", string.Empty) ?? string.Empty;

            return XDocument.Parse(xml);
        }
        catch (XmlException ex)
        {
            throw new WitsmlException(ErrorCodes.InputTemplateNonConforming, ex);
        }
    }
````
- Extensions – methods commonly used for WITSML classes
````C#
    /// <summary>
    /// Converts the <see cref="Timestamp"/> to unix time microseconds.
    /// </summary>
    /// <param name="timestamp">The timestamp.</param>
    /// <returns>The timestamp in unix time microseconds</returns>
    public static long ToUnixTimeMicroseconds(this Timestamp timestamp)
    {
        return ((DateTimeOffset) timestamp).ToUnixTimeMicroseconds();
    }
````

##### PDS.Witsml.Server
Hosts WITSML store service implementation, including service interfaces and high level data provider implementation, including:

- WitsmlDataAdapter – encapsulates basic CRUD functionality for WITSML data objects
````C#
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Well" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Well}" />
    [Export(typeof(IWitsmlDataAdapter<Well>))]
    [Export(typeof(IWitsml141Configuration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class Well141DataAdapter : MongoDbDataAdapter<Well>, IWitsml141Configuration
    {
        ...
````
- WitsmlDataProvider – implements support for WITSML API functions
````C#
    var context = WitsmlOperationContext.Current.Request = request.ToContext();
    var version = string.Empty;
    var dataProvider = Container.Resolve<IWitsmlDataProvider>(new ObjectName(context.ObjectType, version));
    var result = dataProvider.GetFromStore(context);
````
- WitsmlQueryParser – handles parsing of WITSML input in a request
````C#
    var Parser = new WitsmlQueryParser(root, context.ObjectType, context.Options);
    ...
    var logDatas = Parser.Properties("logData").ToArray();
    if (logDatas.Length > 1)
    {
        yield return new ValidationResult(ErrorCodes.RecurringLogData.ToString(), new[] { "LogData" });
    }
````
- EtpDataProvider – implements support for ETP API functions
````C#
    /// <summary>
    /// Initializes a new instance of the <see cref="EtpDataProvider{TObject}"/> class.
    /// </summary>
    /// <param name="container">The composition container.</param>
    /// <param name="dataAdapter">The data adapter.</param>
    protected EtpDataProvider(IContainer container, IWitsmlDataAdapter<TObject> dataAdapter) : base(container, dataAdapter)
    {
    }
    ...
    /// <summary>
    /// Deletes a data object by the specified URI.
    /// </summary>
    /// <param name="uri">The data object URI.</param>
    public virtual void Delete(EtpUri uri)
    {
        DataAdapter.Delete(uri);
    }
````
- WitsmlExtensions – commonly used methods for WITSML classes
````C#
    /// <summary>
    /// Adds support for the specified function and data object to the capServer instance.
    /// </summary>
    /// <param name="capServer">The capServer instance.</param>
    /// <param name="function">The WITSML Store API function.</param>
    /// <param name="dataObject">The data object.</param>
    /// <param name="maxDataNodes">The maximum data nodes.</param>
    /// <param name="maxDataPoints">The maximum data points.</param>
    public static void Add(this Witsml141.CapServer capServer, Functions function, string dataObject, int maxDataNodes, int maxDataPoints)
    {
        Add(capServer, function, new Witsml141Schemas.ObjectWithConstraint(dataObject)
        {
            MaxDataNodes = maxDataNodes,
            MaxDataPoints = maxDataPoints
        });
    }
```` 

##### PDS.Witsml.Server.Integration.Test
Contains integration tests for PDS.Witsml.Server.

##### PDS.Witsml.Server.Web
Implements configuration and security for WITSML and ETP endpoints.

##### PDS.Witsml.Server.UnitTest
Contains unit tests for the solution.

---

### Copyright and License
Copyright &copy; 2016 Petrotechnical Data Systems

Released under the Apache License, Version 2.0

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
> https://github.com/pds-technology/witsml

In accordance with US Export Administration Regulations (EAR) Section 742.15(b), this
source code is not subject to EAR:
 - This source code has been made publicly available in accordance with EAR Section
   734.3(b)(3)(i) by publishing it in accordance with EAR Section 734.7(a)(4) at the above
   URL.
 - The BIS and the ENC Encryption Request Coordinator have been notified via e-mail of this
   URL.

