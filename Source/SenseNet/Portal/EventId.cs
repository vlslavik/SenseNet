namespace SenseNet.Portal
{
    internal static class EventId
    {
        public static class Query
        {
            public static readonly int ParseError = 20001;
        }

        public static class FieldControls
        {
            public static readonly int InitError = 21001;
        }

        public static class Journal
        {
            public static readonly int LogError = 22001;
        }

        public static class Bundling
        {
            public static readonly int ContentLoadError = 24001;
        }

        public static class WebDav
        {
            public static readonly int WebDavError = 22101;
            public static readonly int FolderError = 22111;
        }
        public static readonly int SetCacheControlHeaders = 23001;
    }
}
