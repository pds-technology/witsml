//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.Datatypes;
using Newtonsoft.Json;
using PDS.Framework;
using PDS.Witsml.DataLoader.Properties;

namespace PDS.Witsml.DataLoader
{
    public class Program
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss.ffff";
        private static readonly List<EtpUri> _inserted = new List<EtpUri>();

        private static string _baseDataDirectory = Settings.Default.BaseDataDirectory;
        private static string _dataSchemaVersion = Settings.Default.DataSchemaVersion;
        private static string _dataTypeSettings = Settings.Default.DataTypeSettings;
        private static string _witsmlStoreUrl = Settings.Default.WitsmlStoreUrl;
        private static List<DataTypeInfo> _dataTypes;
        private static IWitsmlClient _client;

        public static void Main(string[] args)
        {
            Console.WriteLine("WITSML Data Loader Started: {0}", DateTime.Now.ToString(DateFormat));
            Console.WriteLine();

            // Process command line arguments
            _baseDataDirectory = GetArgValue(args, "-d") ?? _baseDataDirectory;
            _dataSchemaVersion = GetArgValue(args, "-v") ?? _dataSchemaVersion;
            _dataTypeSettings = GetArgValue(args, "-s") ?? _dataTypeSettings;
            _witsmlStoreUrl = GetArgValue(args, "-u") ?? _witsmlStoreUrl;

            var timer = new Stopwatch();
            timer.Start();

            var proxy = new WITSMLWebServiceConnection(_witsmlStoreUrl, WMLSVersion.WITSML141);
            proxy.Timeout *= 5;

            _client = proxy.CreateClientProxy() as IWitsmlClient;

            var json = File.ReadAllText(_dataTypeSettings);
            _dataTypes = JsonConvert.DeserializeObject<List<DataTypeInfo>>(json);

            foreach (var dataTypeInfo in _dataTypes.Where(x => x.Enabled))
                LoadDataFiles(dataTypeInfo);

            timer.Stop();

            Console.WriteLine("Data Load Completed in {0}", timer.Elapsed);
            Console.ReadKey();
        }

        private static void LoadDataFiles(DataTypeInfo dataTypeInfo)
        {
            Console.WriteLine("Loading {0} data...", dataTypeInfo.ObjectType);
            var count = 0;

            foreach (var folder in Directory.EnumerateDirectories(_baseDataDirectory, dataTypeInfo.FolderNamePattern, SearchOption.AllDirectories))
            {
                foreach (var file in Directory.EnumerateFiles(folder, dataTypeInfo.FileNamePattern, SearchOption.AllDirectories))
                {
                    var timer = new Stopwatch();
                    timer.Start();

                    var xml = File.ReadAllText(file);
                    var type = ObjectTypes.GetObjectGroupType(dataTypeInfo.ObjectType, _dataSchemaVersion);

                    var document = WitsmlParser.Parse(xml);
                    var dataObject = WitsmlParser.Parse(type, document.Root);
                    var collection = dataObject as IEnergisticsCollection;

                    // Unwrap from plural element
                    dataObject = collection?.Items?[0] ?? dataObject;

                    var uri = (dataObject as IWellboreObject)?.GetUri()
                        ?? (dataObject as IWellObject)?.GetUri()
                        ?? (dataObject as IDataObject)?.GetUri()
                        ?? (dataObject as AbstractObject).GetUri();

                    try
                    {
                        var parseSeconds = timer.Elapsed.TotalSeconds;
                        string action;
                        string suppMsgOut;
                        short returnCode;
                        timer.Restart();

                        if (_inserted.Contains(uri) || Exists(dataTypeInfo.ObjectType, xml))
                        {
                            returnCode = _client.WMLS_UpdateInStore(dataTypeInfo.ObjectType, xml, string.Empty, string.Empty, out suppMsgOut);
                            action = "Updated";
                        }
                        else
                        {
                            returnCode = _client.WMLS_AddToStore(dataTypeInfo.ObjectType, xml, string.Empty, string.Empty, out suppMsgOut);
                            action = "Added";
                        }

                        timer.Stop();
                        _inserted.Add(uri);
                        var fileInfo = new FileInfo(file);

                        Console.WriteLine("{0}) {1} {2}; Return Code: {3}; Parsed: {4} sec; Processed: {5} sec", ++count, action, uri, returnCode, parseSeconds, timer.Elapsed.TotalSeconds);
                        Console.WriteLine("      Path: {0}", fileInfo.DirectoryName);
                        Console.WriteLine("      Name: {0}; Size: {1:N2} KB", fileInfo.Name, fileInfo.Length / 1000.0);

                        var log13 = dataObject as Witsml131.Log;
                        if (log13 != null)
                        {
                            Console.WriteLine("      Curves: {0}; Nodes: {1}", log13.LogCurveInfo?.Count, log13.LogData?.Count);
                        }

                        var log14 = dataObject as Witsml141.Log;
                        if (log14 != null)
                        {
                            Console.WriteLine("      Curves: {0}; Nodes: {1}", log14.LogCurveInfo?.Count, log14.LogData?.SelectMany(x => x.Data).Count());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Error: {0}", ex.Message);
                        Console.WriteLine("File: {0}", file);
                        Console.WriteLine("URI: {0}", uri);
                        Console.WriteLine(ex);
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine("Loaded {0} {1} files.", count, dataTypeInfo.ObjectType);
            Console.WriteLine();
        }

        private static bool Exists(string objectType, string xml)
        {
            var document = XDocument.Parse(xml);

            if (document.Root?.FirstNode == null)
                return false;

            var element = document.Root.Elements().First();
            element.RemoveNodes();

            string suppMsgOut, xmlOut;
            _client.WMLS_GetFromStore(objectType, document.ToString(), OptionsIn.ReturnElements.IdOnly, string.Empty, out xmlOut, out suppMsgOut);

            return WitsmlParser.Parse(xmlOut).Root?.FirstNode != null;
        }

        private static string GetArgValue(string[] args, string key)
        {
            if (args == null) return null;

            var values = args
                .Select((x, i) => new { Key = x, Value = i })
                .ToDictionary(x => x.Key, x => x.Value);

            var arg = values
                .FirstOrDefault(x => x.Key.EqualsIgnoreCase(key));

            if (!string.IsNullOrWhiteSpace(arg.Key) && values.Count > arg.Value + 1)
            {
                return values.Where(x => x.Value == arg.Value + 1)
                    .Select(x => x.Key.Replace("\"", string.Empty))
                    .FirstOrDefault();
            }

            return null;
        }
    }
}
