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
    public static partial class ObjectNames
    {
        /// <summary>
        /// The data object name for a 1.4.1.1 Attachment.
        /// </summary>
        public static readonly ObjectName Attachment141 = new ObjectName(ObjectTypes.Attachment, Version141);

        /// <summary>
        /// The data object name for a 2.0 Attachment.
        /// </summary>
        public static readonly ObjectName Attachment200 = new ObjectName(ObjectTypes.Attachment, Version200);

        /// <summary>
        /// The data object name for a 1.3.1.1 Log.
        /// </summary>
        public static readonly ObjectName Log131 = new ObjectName(ObjectTypes.Log, Version131);

        /// <summary>
        /// The data object name for a 1.4.1.1 Log.
        /// </summary>
        public static readonly ObjectName Log141 = new ObjectName(ObjectTypes.Log, Version141);

        /// <summary>
        /// The data object name for a 2.0 Log.
        /// </summary>
        public static readonly ObjectName Log200 = new ObjectName(ObjectTypes.Log, Version200);

        /// <summary>
        /// The data object name for a 1.3.1.1 Message.
        /// </summary>
        public static readonly ObjectName Message131 = new ObjectName(ObjectTypes.Message, Version131);

        /// <summary>
        /// The data object name for a 1.4.1.1 Message.
        /// </summary>
        public static readonly ObjectName Message141 = new ObjectName(ObjectTypes.Message, Version141);

        /// <summary>
        /// The data object name for a 1.3.1.1 Rig.
        /// </summary>
        public static readonly ObjectName Rig131 = new ObjectName(ObjectTypes.Rig, Version131);

        /// <summary>
        /// The data object name for a 1.4.1.1 Rig.
        /// </summary>
        public static readonly ObjectName Rig141 = new ObjectName(ObjectTypes.Rig, Version141);

        /// <summary>
        /// The data object name for a 2.0 Rig.
        /// </summary>
        public static readonly ObjectName Rig200 = new ObjectName(ObjectTypes.Rig, Version200);

        /// <summary>
        /// The data object name for a 1.3.1.1 Trajectory.
        /// </summary>
        public static readonly ObjectName Trajectory131 = new ObjectName(ObjectTypes.Trajectory, Version131);

        /// <summary>
        /// The data object name for a 1.4.1.1 Trajectory.
        /// </summary>
        public static readonly ObjectName Trajectory141 = new ObjectName(ObjectTypes.Trajectory, Version141);

        /// <summary>
        /// The data object name for a 2.0 Trajectory.
        /// </summary>
        public static readonly ObjectName Trajectory200 = new ObjectName(ObjectTypes.Trajectory, Version200);

        /// <summary>
        /// The data object name for a 1.3.1.1 WbGeometry.
        /// </summary>
        public static readonly ObjectName WbGeometry131 = new ObjectName(ObjectTypes.WbGeometry, Version131);

        /// <summary>
        /// The data object name for a 1.4.1.1 WbGeometry.
        /// </summary>
        public static readonly ObjectName WbGeometry141 = new ObjectName(ObjectTypes.WbGeometry, Version141);

        /// <summary>
        /// The data object name for a 1.3.1.1 Well.
        /// </summary>
        public static readonly ObjectName Well131 = new ObjectName(ObjectTypes.Well, Version131);

        /// <summary>
        /// The data object name for a 1.4.1.1 Well.
        /// </summary>
        public static readonly ObjectName Well141 = new ObjectName(ObjectTypes.Well, Version141);

        /// <summary>
        /// The data object name for a 2.0 Well.
        /// </summary>
        public static readonly ObjectName Well200 = new ObjectName(ObjectTypes.Well, Version200);

        /// <summary>
        /// The data object name for a 1.3.1.1 Wellbore.
        /// </summary>
        public static readonly ObjectName Wellbore131 = new ObjectName(ObjectTypes.Wellbore, Version131);

        /// <summary>
        /// The data object name for a 1.4.1.1 Wellbore.
        /// </summary>
        public static readonly ObjectName Wellbore141 = new ObjectName(ObjectTypes.Wellbore, Version141);

        /// <summary>
        /// The data object name for a 2.0 Wellbore.
        /// </summary>
        public static readonly ObjectName Wellbore200 = new ObjectName(ObjectTypes.Wellbore, Version200);

    }
}