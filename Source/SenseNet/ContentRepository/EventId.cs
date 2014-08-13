using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository
{
    public static class EventId
    {
        /* System */
        public static class SystemStart
        {
            public static readonly int ClusterMemberInfoCreated = 111;
            public static readonly int NodeObserversInstantiated = 112;
            public static readonly int MembershipProviderInstantiated = 113;
        }
        public static class TypeSystem
        {
            public static readonly int ContentTypeManagerReset = 11001;
        }

        /* Content repository components */
        public static class Indexing
        {
            public static readonly int SuccessfullyRestored = 15001;
            public static readonly int ExecuteUnprocessedActivitiesFromCommitPoint = 15101;
            public static readonly int ExecuteUnprocessedActivitiesAfterPause = 15102;
            public static readonly int ExecutingUnprocessedActivitiesFinished = 15109;

            public static readonly int IFilterError = 15201;
            public static readonly int BinaryIsTooLarge = 15251;
            public static readonly int FieldIndexingError = 15301;
        }
        public static class Querying
        {
            public static readonly int CannotExecuteQuery = 16001;
        }
        public static class Resource
        {
            public static readonly int ManagerReset = 18001;
        }
        public static class Preview
        {
            public static readonly int PageCountFolderCreated = 19001;
            public static readonly int PageCountFileSaveError = 19101;
            public static readonly int ShellError = 19111;
            public static readonly int PreviewGenerationStatusError = 19201;
            public static readonly int PreviewGenerationError = 19211;
            public static readonly int PreviewGenerationPageError = 19501;            
            public static readonly int RemoteAccessError = 19511;
        }

        /* Errors */
        public static class Error
        {
            public static readonly int UnhandledException = 60001;
        }
    }
}
