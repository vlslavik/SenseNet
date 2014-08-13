using SenseNet.BackgroundOperations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNetTaskAgentService
{
    internal class Logger : ILogger
    {
        internal const string SOURCENAME = "BackgroundOperations";
        internal const string LOGNAME = "SenseNet";
        private static readonly string LOGPREFIX = "#SenseNetTaskAgentService> ";

        private static EventLog _eventLog;
        internal static void Initialize(EventLog eventLog)
        {
            if (!System.Diagnostics.EventLog.SourceExists(Logger.SOURCENAME))
                System.Diagnostics.EventLog.CreateEventSource(Logger.SOURCENAME, LOGNAME);
            eventLog.Source = Logger.SOURCENAME;
            _eventLog = eventLog;
        }

        public static void WriteVerbose(string message)
        {
            Debug.WriteLine(LOGPREFIX + message);
        }
        public static void WriteInformation(string message, int eventId)
        {
            WriteVerbose(message);
            WriteToEventLog(message, EventLogEntryType.Information, eventId);
        }
        public static void WriteError(string message, Exception ex)
        {
            var msg = message + (ex == null ? string.Empty : ex.ToString().Replace(Environment.NewLine, "   "));
            WriteVerbose("ERROR: " + msg);
            WriteToEventLog(msg, EventLogEntryType.Error, EventId.GeneralError);
        }

        private static void WriteToEventLog(string message, EventLogEntryType entryType, int eventId)
        {
            _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId);
        }

        //====================================================================================== ILogger implementation

        void ILogger.WriteVerbose(string message)
        {
            Logger.WriteVerbose(message);
        }

        void ILogger.WriteInformation(string message, int eventId)
        {
            Logger.WriteInformation(message, eventId);
        }

        void ILogger.WriteError(string message, Exception ex)
        {
            Logger.WriteError(message, ex);
        }
    }
}
