//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System.Xml.Linq;

namespace PDS.WITSMLstudio.Query
{
    /// <summary>
    /// QueryTemplates
    /// </summary>
    public partial class QueryTemplates
    {
        /// <summary>
        /// Gets the template for Witsml object.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="returnElementsOptionIn">The return elements option in.</param>
        /// <returns>The XDocument.</returns>
        public static XDocument GetTemplate(string objectType, string version, OptionsIn.ReturnElements returnElementsOptionIn)
        {
            var documentTemplate = new XDocument();

            if (OptionsIn.DataVersion.Version131.Equals(version))
            {
                documentTemplate = GetTemplateForWitsml131(objectType, returnElementsOptionIn);
            }

            if (OptionsIn.DataVersion.Version141.Equals(version))
            {
                documentTemplate = GetTemplateForWitsml141(objectType, returnElementsOptionIn);
            }

            if (documentTemplate.Root != null) return documentTemplate;

            // Unsupported objects
            var type = ObjectTypes.GetObjectGroupType(objectType, version);

            if (OptionsIn.ReturnElements.IdOnly.Equals(returnElementsOptionIn.Value))
            {
                documentTemplate = _template.Create(type);
                _template.RemoveAll(documentTemplate, "/*/*/*[name() != 'name' and name() != 'nameWell' and name() != 'nameWellbore']");
            }
            else
            {
                documentTemplate = _template.Create(type);
            }

            return documentTemplate;
        }
    }
}
