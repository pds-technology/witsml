namespace PDS.Witsml
{
    public enum ErrorCodes : short
    {
        // TODO: finish defining enum members
        // TODO: add error descriptions to Resources.resx

        /// <summary>
        /// Unset
        /// </summary>
        Unset = 0,

        /// <summary>
        /// 1 : Function completed successfully
        /// </summary>
        Success = 1,

        /// <summary>
        /// 2 : Partial success: Function completed successfully but some growing data-object data-nodes were not returned.
        /// </summary>
        ParialSuccess = 2,

        /// <summary>
        /// -401 : The input template MUST contain a plural root element.
        /// </summary>
        MissingPluralRootElement = -401,

        /// <summary>
        /// -402 The value of the OptionsIn keyword of ‘maxReturnNodes’ MUST be greater than zero.
        /// </summary>
        InvalidMaxReturnNodes = -402,

        /// <summary>
        /// -403 A template must include a default namespace declaration for the WITSML namespace.
        /// </summary>
        MissingDefaultWitsmlNamespace = -403,

        /// <summary>
        /// -404 In the value of the capClient schemaVersion, the oldest Data Schema Version must be listed first,
        /// followed by the next oldest, etc.
        /// </summary>
        InvalidClientSchemaVersion = -404,

        /// <summary>
        /// -405 For WMLS_AddToStore, a data-object with the same type and unique identifier(s) must NOT already
        /// exist in the persistent store.
        /// </summary>
        DataObjectUidAlreadyExists = -405,

        /// <summary>
        /// -406 For WMLS_AddToStore, all parentage-pointers and lower level(child) uid values must be defined in the
        /// XMLin file.
        /// </summary>
        MissingParentUid = -406,

        /// <summary>
        /// -407 A non-empty value must be defined for WMLtypeIn.
        /// </summary>
        MissingWMLtypeIn = -407,

        /// <summary>
        /// -408 A non-empty value must be defined for the input template.
        /// </summary>
        MissingInputTemplate = -408,

        /// <summary>
        /// -409 The input template must conform to the appropriate derived schema.
        /// </summary>

        /// <summary>
        /// -410 A template containing empty values must otherwise conform to the appropriate derived schema.
        /// </summary>

        /// <summary>
        /// -411 The OptionsIn parameter string must be encoded utilizing a subset (semicolon separators and no
        /// whitespace) of the encoding rules for HTML form content type application/x-www-form-urlencoded.
        /// </summary>

        /// <summary>
        /// -412 The result of an add, update, or delete operation must be compliant with the derived write schema after
        /// performing a retrieval of the complete data-object.
        /// </summary>

        /// <summary>
        /// -413 : The data-object must be supported by the server as defined in its capability data-object.
        /// </summary>
        DataObjectNotSupported = -413,

        /// <summary>
        /// -414 In the server configuration for the site, the user must have rights to perform the requested operation on
        /// the data-object.
        /// </summary>

        /// <summary>
        /// -415 The input template must specify the unique identifiers of one data-object to be processed.
        /// </summary>

        /// <summary>
        /// -416 For WMLS_DeleteFromStore, an empty uid attribute must not be specified.
        /// </summary>

        /// <summary>
        /// -417 For WMLS_DeleteFromStore, an empty uom attribute must not be specified.
        /// </summary>

        /// <summary>
        /// -418 For WMLS_DeleteFromStore, if an element with a uid attribute in the schema is specified then it must
        /// also be specified with a value for its uid attribute.
        /// </summary>

        /// <summary>
        /// -419 For WMLS_DeleteFromStore, an empty non-recurring container-element with no unique identifier in the
        /// schema or an empty value for logData must not be specified.
        /// </summary>

        /// <summary>
        /// -420 For WMLS_DeleteFromStore, an empty node must not be specified for a non-recurring element or
        /// attribute that is mandatory in the write schema.
        /// </summary>

        /// <summary>
        /// -421 For WMLS_DeleteFromStore, a recurring element that is mandatory in the write schema must retain at
        /// least one occurrence after the deletion.
        /// </summary>

        /// <summary>
        /// -422 For WMLS_GetBaseMsg, a non-empty value must be specified for ReturnValueIn.
        /// </summary>

        /// <summary>
        /// -423 For WMLS_GetCap, the OptionsIn keyword ‘dataVersion’ must specify a Data Schema Version that is
        /// supported by the server as defined by WMLS_GetVersion.
        /// </summary>
        DataVersionNotSupported = -423,

        /// <summary>
        /// -424 For WMLS_GetCap, the OptionsIn keyword ‘dataVersion’ must be specified.
        /// </summary>
        MissingDataVersion = -424,

        /// <summary>
        /// -425 For WMLS_GetFromStore, the OptionsIn keyword ‘returnElements’ must not specify a value of “headeronly”
        /// or “data-only” for a non-growing data-object.
        /// </summary>

        /// <summary>
        /// -426 The input template must conform to the appropriate derived schema after uncompressing the string.
        /// </summary>

        /// <summary>
        /// -427 The OptionsIn keyword ‘requestObjectSelectionCapability’ with a value other than ‘none’ must not be
        /// specified with any other OptionsIn keyword.
        /// </summary>

        /// <summary>
        /// -428 If the OptionsIn keyword ‘requestObjectSelectionCapability’ is specified with a value other than ‘none’
        /// then QueryIn must be the Minimum Query Template.
        /// </summary>

        /// <summary>
        /// -429 For WMLS_GetFromStore, the logData section must not recur when retrieving data.
        /// </summary>

        /// <summary>
        /// -430 A client must specify an item for data-item selection that the server supports.
        /// </summary>

        /// <summary>
        /// -431 A client must specify an item (element or attribute) for data-object selection that the server supports.
        /// </summary>

        /// <summary>
        /// -432 If cascading deletes are not invoked, a client must only request deletion of bottom level data-objects such
        /// that all child data-objects are deleted before the parent is deleted.
        /// </summary>

        /// <summary>
        /// -433 A data-object with the same type and unique identifier(s) must already exist in the persistent store.
        /// </summary>

        /// <summary>
        /// -434 For updating systematically growing data, if data-nodes are specified then the column-identifiers
        /// (mnemonic) must be specified in the data-column-list.
        /// </summary>

        /// <summary>
        /// -435 Deleted January 2014. Not used in the specification.Redundant to -480.
        /// </summary>

        /// <summary>
        /// -436 When an update adds a new column-identifier to a systematically growing data-object, a structural-range
        /// must not also be specified.
        /// </summary>

        /// <summary>
        /// -437 For WMLS_DeleteFromStore with a systematically growing data-object, a column-identifier must not be
        /// specified in the data-column-list (mnemonicList).
        /// </summary>

        /// <summary>
        /// -438 When multiple selection criteria is are included in a recurring element, the same selection items must
        /// exist in each recurring node.
        /// </summary>

        /// <summary>
        /// -439 When multiple selection criteria is are included in a recurring pattern, an empty value must not be
        /// specified.
        /// </summary>

        /// <summary>
        /// -440 The OptionsIn value must be a recognized keyword for that function.
        /// </summary>

        /// <summary>
        /// -441 The value specified with an OptionsIn keyword must be a recognized value for that keyword.
        /// </summary>

        /// <summary>
        /// -442 A client must not specify an OptionsIn keyword that is not supported by the server.
        /// </summary>

        /// <summary>
        /// -443 The value of the uom attribute is must match an ‘annotation’ attribute from the WITSML Units Dictionary
        /// XML file.
        /// </summary>

        /// <summary>
        /// -444 The input template must not specify more than one data-object.
        /// </summary>

        /// <summary>
        /// -445 For WMLS_UpdateInStore, new elements or attributes must not be empty.
        /// </summary>

        /// <summary>
        /// -446 For WMLS_UpdateInStore, a uom attribute must not be specified unless its corresponding value is
        /// specified.
        /// </summary>

        /// <summary>
        /// -447 When adding or updating curves, column-identifiers must be unique.
        /// </summary>

        /// <summary>
        /// -448 For WMLS_UpdateInStore, if an element with a unique identifier in the schema is specified then the
        /// identifier value must also be specified
        /// </summary>

        /// <summary>
        /// -449 When updating data in a systematically growing data-object the indexCurve must be specified in the
        /// mnemonicList.
        /// </summary>

        /// <summary>
        /// -450 When updating data in a systematically growing data-object, each mnemonic must occur only once in the
        /// mnemonicList.
        /// </summary>

        /// <summary>
        /// -451 When updating data in a systematically growing data-object, the unitList must always be specified.
        /// </summary>

        /// <summary>
        /// -452 When updating data in a systematically growing data-object, the unitList must be populated with units
        /// that match the header.
        /// </summary>

        /// <summary>
        /// -453 For WMLS_AddToStore and WMLS_UpdateInStore, the client must always specify the unit for all
        /// measure data.
        /// </summary>

        /// <summary>
        /// -454 For a particular WMLS_AddToStore call, a client must specify all growing data-object index data in the
        /// same unit of measure.
        /// </summary>

        /// <summary>
        /// -455 All datum elements or attributes for indexes in a growing data-object must implicitly or explicitly point to
        /// the same wellDatum when adding or updating data.
        /// </summary>

        /// <summary>
        /// -456 The client must not attempt to add or update more data than is allowed by the server’s maxDataNodes
        /// and maxDataPoints values.
        /// </summary>

        /// <summary>
        /// -457 If a column-identifier representing the index column is specified then it must be specified first in the datacolumn-
        /// list.
        /// </summary>

        /// <summary>
        /// -458 For growing data-objects, a combination of depth and date-time structural-range indices must not be
        /// specified.
        /// </summary>

        /// <summary>
        /// -459 For systematically growing data-objects, the column-identifier (mnemonic) values must not contain one of
        /// the following special characters: single-quote, double-quote, less than, greater than, forward slash,
        /// backward slash, ampersand, comma.
        /// </summary>

        /// <summary>
        /// -460 For getting systematically growing data-objects, if column-identifiers(mnemonics) are specified in both
        /// the header and data sections then the column-identifiers must be the same in the two sections.
        /// </summary>

        /// <summary>
        /// -461 For getting systematically growing data-objects, if a column-definition(logCurveInfo) section is specified
        /// then a mnemonic element must be specified.
        /// </summary>

        /// <summary>
        /// -462 For getting systematically growing data-objects, if a data-node (logData) section is specified then a
        /// mnemonicList element must be specified.
        /// </summary>

        /// <summary>
        /// -463 For updating systematically growing data, the update data must not contain multiple nodes with the same
        /// index.
        /// </summary>

        /// <summary>
        /// -464 Each lower level child uid value must be unique within the context of its nearest recurring parent node.
        /// </summary>

        /// <summary>
        /// -465 The capClient apiVers value must match the API schema version.
        /// </summary>

        /// <summary>
        /// -466 The CapabilitiesIn XML MUST conform to the API capClient schema.
        /// </summary>

        /// <summary>
        /// -467 The server does not support the API Version provided by the client.
        /// </summary>

        /// <summary>
        /// -468 A QueryIn template must include a version attribute in the plural data-object that defines the Data
        /// Schema Version of the data-object.
        /// </summary>

        /// <summary>
        /// -469 The query template is does not conform to the data schema for this data-object.
        /// </summary>

        /// <summary>
        /// -470 Query not supported.
        /// </summary>

        /// <summary>
        /// -471 (Not Assigned)
        /// </summary>

        /// <summary>
        /// -472 The client product name and/or product version number are missing in the HTTP user-agent field.
        /// WITSML requires this information for each function call.
        /// </summary>
        MissingClientUserAgent = -472,

        /// <summary>
        /// -473 For the capClient object, the values of schemaVersion must match the version attribute used in the plural
        /// data-objects.
        /// </summary>

        /// <summary>
        /// -474 For growing data-objects in a query, if a node-index value is to be returned, then the client must explicitly
        /// specify it in the query(which is true for any element or attribute in a query).
        /// </summary>

        /// <summary>
        /// -475 No subset of a growing data-object is specified.A query must specify a subset of one growing dataobject
        /// per query, but multiple individual queries may be included inside the query plural data-object.
        /// </summary>

        /// <summary>
        /// -476 For WMLS_GetFromStore: OptionsIn keyword returnsElements=latest-change-only can only be used for
        /// a changeLog object.
        /// </summary>

        /// <summary>
        /// -477 For getting systematically growing data-objects, both column-description and data sections must be
        /// specified.
        /// </summary>

        /// <summary>
        /// -478 Incorrect case on parent uid.
        /// </summary>

        /// <summary>
        /// -479 Unable to decompress query.
        /// </summary>

        /// <summary>
        /// -480 A new column(log curve) cannot be added at the same time an existing column(log curve) is being
        /// updated. (The index column (curve) does not count as being updated.)
        /// </summary>

        /// <summary>
        /// -481 Parent does not exist.
        /// </summary>
        MissingParentDataObject = -481,

        /// <summary>
        /// -482 Duplicate mnemonics in a query are not allowed.
        /// </summary>

        /// <summary>
        /// -483 The XMLin document does not comply with the update schema.
        /// </summary>

        /// <summary>
        /// -484 A mandatory write schema item is missing.
        /// </summary>
        MissingRequiredData = -484,

        /// <summary>
        /// -485 The client is polling too fast.
        /// </summary>

        /// <summary>
        /// -486 In WMLS_AddToStore, the WMLtypeIn objectType must match the XMLin objectType. Currently, they do not match.
        /// </summary>
        DataObjectTypesDontMatch = -486,

        /// <summary>
        /// -487 In WMLS_AddToStore, the objectType being added in WMLtypeIn must be an objectType supported by
        /// the server.The server does not support the object type trying to be added.
        /// </summary>
        DataObjectTypeNotSupported = -487,

        /// <summary>
        /// -1001 Error adding to data store
        /// </summary>
        ErrorAddingToDataStore = -1001,

        /// <summary>
        /// -1002 Error reading from data store
        /// </summary>
        ErrorReadingFromDataStore = -1002,

        /// <summary>
        /// -1003 Error updating in data store
        /// </summary>
        ErrorUpdatingInDataStore = -1003,

        /// <summary>
        /// -1004 Error deleting from data store
        /// </summary>
        ErrorDeletingFromDataStore = -1004,
    }
}