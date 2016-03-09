namespace PDS.Witsml
{
    /// <summary>
    /// Defines the supported list of version-qualified data object names.
    /// </summary>
    public static class ObjectNames
    {
        private static readonly string Version131 = OptionsIn.DataVersion.Version131.Value;
        private static readonly string Version141 = OptionsIn.DataVersion.Version141.Value;
        private static readonly string Version200 = OptionsIn.DataVersion.Version200.Value;

        public static readonly ObjectName Well131 = new ObjectName(ObjectTypes.Well, Version131);
        public static readonly ObjectName Well141 = new ObjectName(ObjectTypes.Well, Version141);
        public static readonly ObjectName Well200 = new ObjectName(ObjectTypes.Well, Version200);

        public static readonly ObjectName Wellbore131 = new ObjectName(ObjectTypes.Wellbore, Version131);
        public static readonly ObjectName Wellbore141 = new ObjectName(ObjectTypes.Wellbore, Version141);
        public static readonly ObjectName Wellbore200 = new ObjectName(ObjectTypes.Wellbore, Version200);

        public static readonly ObjectName Log131 = new ObjectName(ObjectTypes.Log, Version131);
        public static readonly ObjectName Log141 = new ObjectName(ObjectTypes.Log, Version141);
        public static readonly ObjectName Log200 = new ObjectName(ObjectTypes.Log, Version200);

        public static readonly ObjectName ChannelSet200 = new ObjectName(ObjectTypes.ChannelSet, Version200);
        public static readonly ObjectName Channel200 = new ObjectName(ObjectTypes.Channel, Version200);
    }
}
