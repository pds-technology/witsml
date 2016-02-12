using System;

namespace Energistics.Protocol
{
    [Flags]
    public enum MessageFlags : int
    {
        None = 0x0,
        MultiPart = 0x1,
        FinalPart = 0x2
    }
}
