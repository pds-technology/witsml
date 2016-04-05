//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
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

namespace Energistics.Protocol
{
    public static class MessageTypes
    {
        public enum Core
        {
            RequestSession = 1,
            OpenSession = 2,
            CloseSession = 5,
            ProtocolException = 1000,
            Acknowledge = 1001
        }

        public enum ChannelStreaming
        {
            Start = 0,
            ChannelDescribe,
            ChannelMetadata,
            ChannelData,
            ChannelStreamingStart,
            ChannelStreamingStop,
            ChannelDataChange,
            ChannelNotUsed,
            ChannelDelete,
            ChannelRangeRequest,
            ChannelStatusChange
        }

        public enum ChannelDataFrame
        {
            RequestChannelData = 1,
            ChannelNotUsed,
            ChannelMetadata,
            ChannelDataFrameSet
        }

        public enum Discovery
        {
            GetResources = 1,
            GetResourcesResponse
        }

        public enum Store
        {
            GetObject = 1,
            PutObject,
            DeleteObject,
            Object
        }

        public enum StoreNotification
        {
        }

        public enum GrowingObject
        {
        }

        public enum DataArray
        {
        }

        public enum Query
        {
        }
    }
}
