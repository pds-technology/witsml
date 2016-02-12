using Avro.Specific;

namespace Energistics.Common
{
    public delegate void ProtocolEventHandler<T>(object sender, ProtocolEventArgs<T> e) where T : ISpecificRecord;

    public delegate void ProtocolEventHandler<T, V>(object sender, ProtocolEventArgs<T, V> e) where T : ISpecificRecord;
}
