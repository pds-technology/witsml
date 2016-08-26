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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess.WITSML131;
using PDS.Framework;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Provides validation for <see cref="Log" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Log}" />
    [Export(typeof(IDataObjectValidator<Log>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Log131Validator : DataObjectValidator<Log>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Log131Validator" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="logDataAdapter">The log data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        [ImportingConstructor]
        public Log131Validator(IContainer container, IWitsmlDataAdapter<Log> logDataAdapter, IWitsmlDataAdapter<Wellbore> wellboreDataAdapter, IWitsmlDataAdapter<Well> wellDataAdapter) : base(container)
        {
            Context.Ignored = new List<string> {"logData", "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex",
                "minIndex", "maxIndex", "minDateTimeIndex", "maxDateTimeIndex", };
        }

        /// <summary>
        /// Validate the uid attribute value of the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The value of the uid attribute.</returns>
        /// <exception cref="WitsmlException">
        /// </exception>
        protected override string GetAndValidateArrayElementUid(XElement element)
        {
            var uidAttribute = element.Attributes().FirstOrDefault(a => a.Name == "uid");
            if (uidAttribute != null)
            {
                if (!string.IsNullOrEmpty(uidAttribute.Value))
                    return uidAttribute.Value;

                if (Context.Function != Functions.DeleteFromStore)
                    throw new WitsmlException(Context.Function.GetMissingElementUidErrorCode());
                throw new WitsmlException(ErrorCodes.EmptyUidSpecified);
            }
            if (Context.Function != Functions.DeleteFromStore)
                return null;
            if (element.Name.LocalName != "logCurveInfo" || !DeleteChannelData(element))
                throw new WitsmlException(ErrorCodes.MissingElementUidForDelete);

            return null;
        }

        private bool DeleteChannelData(XElement element)
        {
            var fields = new List<string> { "mnemonic", "minDateTimeIndex", "maxDateTimeIndex", "minIndex", "maxIndex" };
            if (!element.HasElements)
                return false;

            return element.Elements().All(e => fields.Contains(e.Name.LocalName));
        }
    }
}
