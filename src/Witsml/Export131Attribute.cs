using System;
using System.ComponentModel.Composition;

namespace PDS.Witsml
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class Export131Attribute : ExportAttribute
    {
        private static readonly string Version = OptionsIn.DataVersion.Version131.Value;

        public Export131Attribute(string contractName) : base(new ObjectName(contractName, Version))
        {
        }

        public Export131Attribute(string contractName, Type contractType) : base(new ObjectName(contractName, Version), contractType)
        {
        }
    }
}
