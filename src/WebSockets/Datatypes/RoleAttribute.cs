using System;

namespace Energistics.Datatypes
{
    public sealed class RoleAttribute : Attribute
    {
        public RoleAttribute(string senderRole)
        {
            SenderRole = senderRole;
        }

        public string SenderRole { get; private set; }
    }
}
