namespace Energistics.Datatypes
{
    public static class UriFormats
    {
        public static class Witsml141
        {
            public const string Root = "eml://witsml141";

            public const string Wells = Root + "/well";
            public const string Well = Root + "/well({0})";

            public const string Wellbores = Root + "/well({0})/wellbore";
            public const string Wellbore = Root + "/well({0})/wellbore({1})";

            public const string Logs = Root + "/well({0})/wellbore({1})/log";
            public const string Log = Root + "/well({0})/wellbore({1})/log({2})";

            public const string LogCurves = Root + "/well({0})/wellbore({1})/log({2})/curve";
            public const string LogCurve = Root + "/well({0})/wellbore({1})/log({2})/curve({3})";
        }

        public static class Witsml200
        {
            public const string Root = "eml://witsml20";
        }
    }
}
