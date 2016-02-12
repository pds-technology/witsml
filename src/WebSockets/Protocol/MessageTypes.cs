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
