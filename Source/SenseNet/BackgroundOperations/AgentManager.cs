using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.BackgroundOperations
{
    public class AgentManagerEventArgs : EventArgs
    {
        public AgentManagerEventArgs(Process process) { Process = process; }
        public Process Process { get; private set; }
    }

    public class AgentManager
    {
        public static readonly string AGENT_PROCESSNAME = "SenseNetTaskAgent";
        private static Timer _agentTimer;
        private static int _counter;
        private static ILogger _logger;
        private static string _executionBasePath;
        private static Process[] _agentProcesses;

        public static event EventHandler<AgentManagerEventArgs> ProcessStarted;

        //====================================================================================== Service methods

        /// <summary>
        /// Start monitoring and reviving task executor agents.
        /// </summary>
        /// <param name="executionBasePath">The absolute path of the folder where the code is executing. This will be used for finding the agent executable if its configured path is relative.</param>
        /// <param name="logger">Helper object for logging.</param>
        public static void Startup(string executionBasePath, ILogger logger)
        {            
            _logger = logger;
            _executionBasePath = executionBasePath;

            _agentProcesses = new Process[Configuration.TaskAgentCount];

            _agentTimer = new Timer(new TimerCallback(HeartBeatTimerElapsed), null, 0, 5000);
        }

        public static void Shutdown()
        {
            ShutDownAgentProcess();
        }

        //====================================================================================== Agent manager methods

        private static void EnsureAgentProcess()
        {
            var startedCount = 0;

            try
            {
                for (var i = 0; i < _agentProcesses.Length; i++)
                {
                    if (_agentProcesses[i] == null || _agentProcesses[i].HasExited)
                    {
                        // start a new process, but do not wait for it
                        _agentProcesses[i] = Process.Start(new ProcessStartInfo(Configuration.AgentPath));
                        startedCount++;

                        // notify outsiders
                        if (ProcessStarted != null)
                            ProcessStarted(null, new AgentManagerEventArgs(_agentProcesses[i]));
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                    _logger.WriteError("Agent start error. Agent path: " + Configuration.AgentPath + ". ", ex);

                return;
            }

            if (startedCount > 0)
            {
                if (_logger != null)
                    _logger.WriteInformation(string.Format("{0} STARTED ({1} new instance(s) from {2}).", AGENT_PROCESSNAME, startedCount, Configuration.AgentPath), EventId.AgentStarted);
            }
            else if (++_counter >= 10)
            {
                _counter = 0;

                if (_logger != null)
                    _logger.WriteVerbose(string.Format("{0} is running ({1} instance(s) from {2}).", AGENT_PROCESSNAME, Configuration.TaskAgentCount, Configuration.AgentPath));
            }
        }

        private static void ShutDownAgentProcess()
        {
            if (_agentProcesses == null)
                return;

            var stopCount = 0;

            foreach (var agentProcess in _agentProcesses.Where(p => p != null && !p.HasExited))
            {
                agentProcess.Kill();
                stopCount++;
            }

            if (stopCount > 0 && _logger != null)
                _logger.WriteVerbose(string.Format("{0} instances of the {1} process were killed during shutdown.", stopCount, AGENT_PROCESSNAME));
        }

        //====================================================================================== Helper methods

        private static void HeartBeatTimerElapsed(object o)
        {
            EnsureAgentProcess();
        }

        private static class Configuration
        {
            private const string TASKAGENTCOUNTKEY = "TaskAgentCount";
            private const int DEFAULTTASKAGENTCOUNT = 1;
            private static int? _taskAgentCount;
            public static int TaskAgentCount
            {
                get
                {
                    if (!_taskAgentCount.HasValue)
                    {
                        int value;
                        if (!int.TryParse(ConfigurationManager.AppSettings[TASKAGENTCOUNTKEY], out value) || value < 1)
                            value = DEFAULTTASKAGENTCOUNT;
                        _taskAgentCount = value;
                    }

                    return _taskAgentCount.Value;
                }
            }

            private const string AGENTPATHKEY = "AgentPath";
            private const string DEFAULTAGENTPATH = ".\\SenseNetTaskAgent.exe";
            private static string _agentPath;
            public static string AgentPath
            {
                get
                {
                    if (_agentPath == null)
                    {
                        var value = ConfigurationManager.AppSettings[AGENTPATHKEY];
                        if (string.IsNullOrEmpty(value))
                            value = DEFAULTAGENTPATH;

                        // the configured path can be absolute or relative
                        _agentPath = Path.IsPathRooted(value) 
                            ? value
                            : Path.GetFullPath(Path.Combine(AgentManager._executionBasePath, value));
                    }
                    return _agentPath;
                }
            }
        }
    }
}
