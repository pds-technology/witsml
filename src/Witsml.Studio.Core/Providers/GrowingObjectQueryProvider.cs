//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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

using System.Linq;
using System.Xml.Linq;

namespace PDS.Witsml.Studio.Core.Providers
{
    public class GrowingObjectQueryProvider
    {
        /// <summary>
        /// Updates the data query.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="queryIn">The query in.</param>
        /// <param name="xmlOut">The XML out.</param>
        /// <returns></returns>
        public string UpdateDataQuery(string objectType, string queryIn, string xmlOut)
        {
            var queryDoc = WitsmlParser.Parse(queryIn);
            var resultDoc = WitsmlParser.Parse(xmlOut);
            var ns = queryDoc.Root.GetDefaultNamespace();

            var queryLog = queryDoc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "log");
            var resultLog = resultDoc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "log");

            if (queryLog != null && resultLog != null)
            {
                var endIndex = resultLog = resultLog.Elements().FirstOrDefault(e => e.Name.LocalName == "endIndex");
                if (endIndex != null)
                {
                    var startIndex = queryLog.Elements().FirstOrDefault(e => e.Name.LocalName == "startIndex");
                    if (startIndex != null)
                    {
                        startIndex.Value = endIndex.Value;
                    }
                    else
                    {
                        var startIndexElement = new XElement(ns + "startIndex", endIndex.Value);
                        foreach (var attribute in endIndex.Attributes())
                        {
                            startIndexElement.SetAttributeValue(attribute.Name, attribute.Value);
                        }
                        queryLog.Add(startIndexElement);
                    }

                    return queryDoc.ToString();
                }

                var endDateTimeIndex = resultLog = resultLog.Elements().FirstOrDefault(e => e.Name.LocalName == "endDateTimeIndex");
                if (endDateTimeIndex != null)
                {
                    var startDateTimeIndex = queryLog.Elements().FirstOrDefault(e => e.Name.LocalName == "startDateTimeIndex");
                    if (startDateTimeIndex != null)
                        startDateTimeIndex.Value = endDateTimeIndex.Value;
                    else
                        queryLog.Add(new XElement(ns + "startDateTimeIndex", endDateTimeIndex.Value));

                    return queryDoc.ToString();
                }
            }

            return string.Empty;
        }
    }
}
