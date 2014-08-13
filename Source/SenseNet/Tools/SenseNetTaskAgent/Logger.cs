using SenseNet.BackgroundOperations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNetTaskAgent
{
    internal enum LogCategory {General, Communication};

    internal class Logger
    {
        internal const string SOURCENAME = "BackgroundOperations";
        internal const string LOGNAME = "SenseNet";
        private static readonly string LOGPREFIX = "#TaskAgent> ";
        private static readonly string COMMLOGPREFIX = "#SignalR> ";

        private static EventLog _eventLog;
        internal static void Initialize()
        {
            EventLog eventLog = new EventLog(LOGNAME);
            eventLog.Source = SOURCENAME;

            if (!System.Diagnostics.EventLog.SourceExists(Logger.SOURCENAME))
                System.Diagnostics.EventLog.CreateEventSource(Logger.SOURCENAME, LOGNAME);
            eventLog.Source = Logger.SOURCENAME;
            _eventLog = eventLog;
        }

        internal static void WriteVerbose(string message, params object[] args)
        {
            Write(LogLevel.Verbose, LogCategory.General, 0, String.Format(message, args));
        }
        internal static void WriteInformation(int eventId, string message, params object[] args)
        {
            Write(LogLevel.Information, LogCategory.General, eventId, String.Format(message, args));
        }
        internal static void WriteWarning(int eventId, string message, params object[] args)
        {
            Write(LogLevel.Warning, LogCategory.General, eventId, String.Format(message, args));
        }
        internal static void WriteError(int eventId, Exception e)
        {
            Console.WriteLine(LOGPREFIX + "ERROR: " + e.ToString());
            Debug.WriteLine(LOGPREFIX + "ERROR: " + e.ToString());
            Write(LogLevel.Error, LogCategory.General, eventId, e.ToString());
        }

        internal static void WriteVerbose(LogCategory category, string message, params object[] args)
        {
            Write(LogLevel.Verbose, category, 0, String.Format(message, args));
        }
        internal static void WriteInformation(LogCategory category, int eventId, string message, params object[] args)
        {
            Write(LogLevel.Information, category, eventId, String.Format(message, args));
        }
        internal static void WriteWarning(LogCategory category, int eventId, string message, params object[] args)
        {
            Write(LogLevel.Warning, category, eventId, String.Format(message, args));
        }
        internal static void WriteError(LogCategory category, int eventId, Exception e)
        {
            var prefix = "An error occured on the agent " + Agent.AgentName + ": ";
            Console.WriteLine(LOGPREFIX + prefix + e.ToString());
            Debug.WriteLine(LOGPREFIX + prefix + e.ToString());
            Write(LogLevel.Error, category, eventId, prefix + e.ToString());
        }


        //internal static void LogCommunicationMessage(string message, params object[] args)
        //{
        //    Debug.WriteLine(String.Format(COMMLOGPREFIX + message, args));
        //}

        private static void Write(LogLevel level, LogCategory category, int eventId, string message)
        {
            var prefix = string.Empty;
            switch (category)
            {
                case LogCategory.General: prefix = LOGPREFIX; break;
                case LogCategory.Communication: prefix = COMMLOGPREFIX; break;
                default: throw new NotImplementedException("Unknown category: " + category);
            }

            Debug.WriteLine(prefix + message);

            if (level == LogLevel.Verbose)
                return;

            var entryType = level == LogLevel.Information ? EventLogEntryType.Information : level == LogLevel.Warning ? EventLogEntryType.Warning : EventLogEntryType.Error;

            _eventLog.WriteEntry(message, entryType, eventId);

            try
            {
                Agent.InvokeProxy(Hub.OnEvent, new SenseNet.BackgroundOperations.TaskManagerEvent
                {
                    AgentName = Agent.AgentName,
                    EventId = eventId,
                    Level = level,
                    MachineName = Environment.MachineName,
                    Message = message
                });
            }
            catch (Exception e)
            {
                int q = 1;
            }
        }
    }
}
