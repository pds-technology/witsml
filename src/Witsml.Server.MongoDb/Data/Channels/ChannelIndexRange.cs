
namespace PDS.Witsml.Server.Data.Channels
{
    public struct ChannelIndexRange
    {
        public int Start;
        public int End;

        public ChannelIndexRange(int start, int end)
        {
            Start = start;
            End = end;
        }
    }
}
