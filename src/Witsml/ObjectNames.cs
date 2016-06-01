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

namespace PDS.Witsml
{
    /// <summary>
    /// Defines the supported list of version-qualified data object names.
    /// </summary>
    public static class ObjectNames
    {
        private static readonly string Version131 = OptionsIn.DataVersion.Version131.Value;
        private static readonly string Version141 = OptionsIn.DataVersion.Version141.Value;
        private static readonly string Version200 = OptionsIn.DataVersion.Version200.Value;

        /// <summary>
        /// The ObjectName for 131 version wells.
        /// </summary>
        public static readonly ObjectName Well131 = new ObjectName(ObjectTypes.Well, Version131);

        /// <summary>
        /// The ObjectName for 141 version wells.
        /// </summary>
        public static readonly ObjectName Well141 = new ObjectName(ObjectTypes.Well, Version141);

        /// <summary>
        /// The ObjectName for 2.0 version wells.
        /// </summary>
        public static readonly ObjectName Well200 = new ObjectName(ObjectTypes.Well, Version200);

        /// <summary>
        /// The ObjectName for 131 version wellbores.
        /// </summary>
        public static readonly ObjectName Wellbore131 = new ObjectName(ObjectTypes.Wellbore, Version131);

        /// <summary>
        /// The ObjectName for 141 version wellbores.
        /// </summary>
        public static readonly ObjectName Wellbore141 = new ObjectName(ObjectTypes.Wellbore, Version141);

        /// <summary>
        /// The ObjectName for 2.0 version wellbores.
        /// </summary>
        public static readonly ObjectName Wellbore200 = new ObjectName(ObjectTypes.Wellbore, Version200);

        /// <summary>
        /// The ObjectName for 131 version logs.
        /// </summary>
        public static readonly ObjectName Log131 = new ObjectName(ObjectTypes.Log, Version131);

        /// <summary>
        /// The ObjectName for 141 version logs.
        /// </summary>
        public static readonly ObjectName Log141 = new ObjectName(ObjectTypes.Log, Version141);

        /// <summary>
        /// The ObjectName for 2.0 version logs.
        /// </summary>
        public static readonly ObjectName Log200 = new ObjectName(ObjectTypes.Log, Version200);

        /// <summary>
        /// The ObjectName for 2.0 version channel sets.
        /// </summary>
        public static readonly ObjectName ChannelSet200 = new ObjectName(ObjectTypes.ChannelSet, Version200);

        /// <summary>
        /// The ObjectName for 2.0 version channels.
        /// </summary>
        public static readonly ObjectName Channel200 = new ObjectName(ObjectTypes.Channel, Version200);
    }
}
