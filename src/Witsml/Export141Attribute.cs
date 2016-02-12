using System;
using System.ComponentModel.Composition;

namespace PDS.Witsml
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class Export141Attribute : ExportAttribute
    {
        private static readonly string Version = OptionsIn.DataVersion.Version141.Value;

        public Export141Attribute(string contractName) : base(new ObjectName(contractName, Version))
        {
        }

        public Export141Attribute(string contractName, Type contractType) : base(new ObjectName(contractName, Version), contractType)
        {
        }
    }
}
