using System;
using System.ComponentModel.Composition;

namespace PDS.Witsml
{
    /// <summary>
    /// Specifies that a type, property, field, or method provides a particular export for data schema version 1.4.1.1.
    /// </summary>
    /// <seealso cref="System.ComponentModel.Composition.ExportAttribute" />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class Export141Attribute : ExportAttribute
    {
        private static readonly string Version = OptionsIn.DataVersion.Version141.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Export141Attribute"/> class.
        /// </summary>
        /// <param name="contractType">A type from which to derive the contract name that is used to export the type or member marked with this attribute, or null to use the default contract name.</param>
        public Export141Attribute(Type contractType) : base(new ObjectName(Version), contractType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Export141Attribute"/> class.
        /// </summary>
        /// <param name="contractName">The contract name that is used to export the type or member marked with this attribute, or null or an empty string ("") to use the default contract name.</param>
        public Export141Attribute(string contractName) : base(new ObjectName(contractName, Version))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Export141Attribute"/> class.
        /// </summary>
        /// <param name="contractName">The contract name that is used to export the type or member marked with this attribute, or null or an empty string ("") to use the default contract name.</param>
        /// <param name="contractType">The type to export.</param>
        public Export141Attribute(string contractName, Type contractType) : base(new ObjectName(contractName, Version), contractType)
        {
        }
    }
}
