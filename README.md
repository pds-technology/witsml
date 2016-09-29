
## WITSML
The “PDS.Witsml” solution provides reusable components referenced by all PDS WITSML applications containing the following projects: 

##### Framework
Provides the composition container used to resolve dependencies.
<br/>
<br/>
##### Framework Web
Configures the composition container to resolve dependencies for web projects and provides security.
<br/>
<br/>
##### WITSML
Contains basic classes related to WITSML and are referenced by other projects, including but not limiting to the following:
- ChannelDataReader - facilitates parsing and reading of log channel data
- DataObjectNavigator - a framework for navigating a WITSML document
- WitsmlParser - static helper methods to parse WITSML XML strings
- Extension methods – commonly used methods for WITSML classes
<br/>
<br/>
##### WITSML Server
Hosts WITSML store service implementation, including service interfaces and high level data provider implementation, including:
- WitsmlDataAdapter – encapsulates basic CRUD functionality for WITSML data objects
- WitsmlDataProvider – implements support for WITSML API functions
- WitsmlQueryParser – handles parsing of WITSML input in a request
- EtpDataProvider – implements support for ETP API functions
<br/>
<br/>
##### WITSML Server Integration Test
Contains integration tests for PDS.Witsml.Server.
<br/>
<br/>
##### WITSML Server Web
Implements configuration and security for WITSML and ETP endpoints.
<br/>
<br/>
##### WITSML Server Unit Test
Contains unit tests for the solution

---

### Copyright and License
Copyright &copy; 2016 Petrotechnical Data Systems

Released under the Apache License, Version 2.0