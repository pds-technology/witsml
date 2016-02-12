using System;

namespace Energistics.Datatypes
{
    public sealed class RolesAttribute : Attribute
    {
        public RolesAttribute(string serverRole, string clientRole)
        {
            ServerRole = serverRole;
            ClientRole = clientRole;
        }

        public string ServerRole { get; private set; }

        public string ClientRole { get; private set; }
    }
}
