namespace PDS.Witsml
{
    public static class ObjectNames
    {
        private static readonly string Version141 = OptionsIn.DataVersion.Version141.Value;
        private static readonly string Version131 = OptionsIn.DataVersion.Version131.Value;

        public static readonly ObjectName Well131 = new ObjectName(ObjectTypes.Well, Version131);
        public static readonly ObjectName Well141 = new ObjectName(ObjectTypes.Well, Version141);

        public static readonly ObjectName Wellbore131 = new ObjectName(ObjectTypes.Wellbore, Version131);
        public static readonly ObjectName Wellbore141 = new ObjectName(ObjectTypes.Wellbore, Version141);

        public static readonly ObjectName Log131 = new ObjectName(ObjectTypes.Log, Version131);
        public static readonly ObjectName Log141 = new ObjectName(ObjectTypes.Log, Version141);
    }
}
