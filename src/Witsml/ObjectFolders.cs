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
    /// Defines the list of supported data folder names for display in an object hierarchy.
    /// </summary>
    public static class ObjectFolders
    {
        /// <summary>
        /// An ObjectFolders identifier for Logs.
        /// </summary>
        public const string Logs = "Logs";

        /// <summary>
        /// An ObjectFolders identifier for Time.
        /// </summary>
        public const string Time = "Time";

        /// <summary>
        /// An ObjectFolders identifier for Depth.
        /// </summary>
        public const string Depth = "Depth";

        /// <summary>
        /// An ObjectFolders identifier for Other.
        /// </summary>
        public const string Other = "Other";

        /// <summary>
        /// An ObjectFolders identifier for MudLogs.
        /// </summary>
        public const string MudLogs = "MudLogs";

        /// <summary>
        /// An ObjectFolders identifier for Rigs.
        /// </summary>
        public const string Rigs = "Rigs";

        /// <summary>
        /// An ObjectFolders identifier for Trajectories.
        /// </summary>
        public const string Trajectories = "Trajectories";
    }
}
