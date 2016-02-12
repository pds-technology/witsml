using System;

namespace Energistics.Datatypes
{
    public sealed class ProtocolAttribute : Attribute
    {
        public ProtocolAttribute(Protocols protocol)
        {
            Protocol = protocol;
        }

        public Protocols Protocol { get; private set; }
    }
}
