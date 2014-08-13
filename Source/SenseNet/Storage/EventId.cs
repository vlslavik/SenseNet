using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    internal static class EventId
    {
        /* System */
        public static class SystemStart
        {
            public static readonly int ClusterMemberInfoCreated = 111;
            public static readonly int NodeObserversInstantiated = 112;
        }
        public static class TypeSystem
        {
            public static readonly int NodeTypeManagerRestart = 1101;
        }
        public static class Messaging
        {
            public static readonly int ChannelPurge = 1201;
        }
        public static class ClusterChannelMonitor
        {
            public static readonly int ChannelStarted = 1251;
            public static readonly int ChannelStopped = 1252;
        }
        public static class PerformanceCounters
        {
            public static readonly int InitializeFailed = 2001;
        }

        /* Errors */
        public static class Error
        {
            public static readonly int SecurityError = 60002;
            
        }
    }
}
