## WITSML
The “PDS.Witsml” solution provides reusable components referenced by all PDS WITSML applications containing the following projects: 

##### PDS.Framework
Provides the composition container used to resolve dependencies.
<br/>
<br/>
##### PDS.Framework.Web
Configures the composition container to resolve dependencies for web projects and provides security.
<br/>
<br/>
##### PDS.Witsml
Contains basic classes related to WITSML and are referenced by other projects, including but not limiting to the following:
- ChannelDataReader - facilitates parsing and reading of log channel data
````C#
    /// <summary>
    /// Adds ChannelDataChunks using the specified reader.
    /// </summary>
    /// <param name="reader">The <see cref="ChannelDataReader" /> used to parse the data.</param>
    /// <param name="transaction">The transaction.</param>
    /// <exception cref="WitsmlException"></exception>
    public void Add(ChannelDataReader reader, MongoTransaction transaction = null)
    {
        if (reader == null || reader.RecordsAffected <= 0) return;

        try
        {
            BulkWriteChunks(
                ToChunks(
                    reader.AsEnumerable()),
                reader.Uri,
                string.Join(",", reader.Mnemonics),
                string.Join(",", reader.Units),
                string.Join(",", reader.NullValues),
                transaction);

            CreateChannelDataChunkIndex();
        }
        ...
````
- DataObjectNavigator - a framework for navigating a WITSML document
````C#
    /// <summary>
    /// Validates the uom/value pair for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="uomProperty">The uom property.</param>
    /// <param name="measureValue">The measure value.</param>
    /// <returns>The uom value if valid.</returns>
    /// <exception cref="WitsmlException"></exception>
    protected string ValidateMeasureUom(XElement element, PropertyInfo uomProperty, string measureValue)
    {
        var xmlAttribute = uomProperty.GetCustomAttribute<XmlAttributeAttribute>();
        var isRequired = IsRequired(uomProperty);

        // validation not needed if uom attribute is not defined
        if (xmlAttribute == null)
            return null;

        var uomValue = element.Attributes()
            .Where(x => x.Name.LocalName == xmlAttribute.AttributeName)
            .Select(x => x.Value)
            .FirstOrDefault();

        // uom is required when a measure value is specified
        if (isRequired && !string.IsNullOrWhiteSpace(measureValue) && string.IsNullOrWhiteSpace(uomValue))
        {
            throw new WitsmlException(ErrorCodes.MissingUnitForMeasureData);
        }

        return uomValue;
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
    /// Serialize WITSML query results to XML and remove empty elements and xsi:nil attributes.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <returns>The serialized XML string.</returns>
    public static string ToXml(object obj)
    {
        _log.Debug("Serializing object to XML.");

        if (obj == null) return string.Empty;

        var xml = EnergisticsConverter.ObjectToXml(obj);
        var xmlDoc = Parse(xml);
        var root = xmlDoc.Root;

        if (root == null) return string.Empty;

        foreach (var element in root.Elements())
        {
            RemoveEmptyElements(element);
        }

        return root.ToString();
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
    // Validate that uids in LogCurveInfo are unique
    else if (logCurves != null && logCurves.HasDuplicateUids())
    {
        yield return new ValidationResult(ErrorCodes.ChildUidNotUnique.ToString(), new[] { "LogCurveInfo", "Uid" });
    }
```` 
##### PDS.Witsml.Server.Integration.Test
Contains integration tests for PDS.Witsml.Server.
<br/>
<br/>
##### PDS.Witsml.Server.Web
Implements configuration and security for WITSML and ETP endpoints.
<br/>
<br/>
##### PDS.Witsml.Server.UnitTest
Contains unit tests for the solution.

---

### Copyright and License
Copyright &copy; 2016 Petrotechnical Data Systems

Released under the Apache License, Version 2.0