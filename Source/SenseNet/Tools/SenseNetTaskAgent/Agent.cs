using Microsoft.AspNet.SignalR.Client;
using SenseNet.BackgroundOperations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNetTaskAgent
{
    internal class Agent
    {
        private static string _agentName = Guid.NewGuid().ToString();
        internal static string AgentName { get { return _agentName; } }
        private static object _workingsync = new object();
        private static bool _working;
        private static SnTask _currentTask;
        internal static Dictionary<string, string> TaskExecutors { get; private set; }
        private static string[] _capabilities;
        private static ServerContext _serverContext;

        private static HubConnection _hubConnection;
        private static IHubProxy _hubProxy;

        static void Main(string[] args)
        {
            try
            {
                Logger.Initialize();

                DiscoverCapabilities();

                _updateLockPeriodInMilliseconds = Configuration.UpdateLockPeriodInSeconds * 1000;
                _updateLockTimer = new Timer(new TimerCallback(UpdateLockTimerElapsed));

                _watchExecutorPeriodInMilliseconds = Configuration.ExecutorTimeoutInSeconds * 1000;
                _watchExecutorTimer = new Timer(new TimerCallback(WatchExecutorTimerElapsed));

                var started = StartSignalR();

                _heartBeatTimerPeriodInMilliseconds = Configuration.HeartbeatPeriodInSeconds * 1000;
                _heartbeatTimer = new Timer(new TimerCallback(HeartBeatTimerElapsed), null, _heartBeatTimerPeriodInMilliseconds, _heartBeatTimerPeriodInMilliseconds);

                if (started)
                    Work();
                else
                    _reconnecting = true;

                Console.WriteLine("Press <enter> to exit...");
                Console.ReadLine();

                _hubConnection.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteError(EventId.GeneralError, ex);
            }
        }

        private static void DiscoverCapabilities()
        {
            var executors = new Dictionary<string, string>();
            
            foreach(var item in Configuration.ExpliciteExecutors)
                executors.Add(item.Key, item.Value);

            if (Directory.Exists(Configuration.TaskExecutorDirectory))
            {
                foreach (var executorDirectory in Directory.GetDirectories(Configuration.TaskExecutorDirectory))
                {
                    var name = Path.GetFileName(executorDirectory);
                    if (executors.ContainsKey(name))
                        continue;
                    var exe = Path.Combine(executorDirectory, name + ".exe");
                    if (File.Exists(exe))
                        executors.Add(name, exe);
                }
            }

            foreach (var item in executors)
                Logger.WriteVerbose("Agent {0} capability: {1}, {2}", AgentName, item.Key, item.Value);
            _capabilities = executors.Keys.ToArray();
            TaskExecutors = executors;
        }

        private static bool StartSignalR()
        {
            _hubConnection = new HubConnection(Configuration.RepositoryUrl);
            _hubConnection.Closed += Connection_Closed;
            _hubConnection.ConnectionSlow += Connection_ConnectionSlow;
            _hubConnection.Error += Connection_Error;
            _hubConnection.Received += Connection_Received;
            _hubConnection.Reconnected += Connection_Reconnected;
            _hubConnection.Reconnecting += Connection_Reconnecting;
            _hubConnection.StateChanged += Connection_StateChanged;

            // set Authorization header
            _hubConnection.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(Configuration.Username + ":" + Configuration.Password)));

            _hubProxy = _hubConnection.CreateHubProxy(Hub.Name);

            // register methods for incoming messages
            _hubProxy.On<SnTask>("newTask", (task) => NewTask(task));

            ServicePointManager.DefaultConnectionLimit = 10;

            Logger.WriteVerbose(string.Format("Agent {0} is CONNECTING to {1}...", AgentName, _hubConnection.Url));

            try
            {
                _hubConnection.Start().Wait();
                Logger.WriteInformation(EventId.AgentConnected, string.Format("Agent {0} is CONNECTED to {1}.", AgentName, _hubConnection.Url));

                _serverContext = InvokeProxy<ServerContext>(Hub.OnAgentConnected, AgentName).Result;
                var msg = String.Format("Agent {0} works in {1} server context.", AgentName, _serverContext.ServerType.ToString().ToLower());
                Logger.WriteVerbose(msg);
                Console.WriteLine(msg);

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteError(EventId.CommunicationError, ex);
                return false;
            }
        }
        private static void Reconnect()
        {
            Logger.WriteVerbose(LogCategory.Communication, "Agent {0} RECONNECTING...", AgentName);
            _reconnecting = true;
            _hubConnection.Dispose();
            if (!StartSignalR())
            {
                Logger.WriteVerbose(LogCategory.Communication, "Agent {0} NOT RECONNECTED", AgentName);
                return;
            }
            Logger.WriteVerbose(LogCategory.Communication, "Agent {0} RECONNECTED", AgentName);
            _reconnecting = false;
            Work();
        }

        static void Connection_StateChanged(StateChange obj)
        {
            Logger.WriteVerbose(LogCategory.Communication, "Agent {0} connection state changed: {1} --> {2}", AgentName, obj.OldState, obj.NewState);
            if (obj.NewState == ConnectionState.Disconnected)
            {
                _reconnecting = true;
                return;
            }
            if (obj.NewState == ConnectionState.Connected)
            {
                _reconnecting = false;
                return;
            }
        }
        static void Connection_Reconnecting()
        {
            Logger.WriteVerbose(LogCategory.Communication, "Agent {0}: connection reconnecing.", AgentName);
        }
        static void Connection_Reconnected()
        {
            Logger.WriteInformation(LogCategory.Communication, EventId.AgentReconnected, "Agent {0}: connection reconnected.", AgentName);
        }
        static void Connection_Received(string obj)
        {
            Logger.WriteVerbose(LogCategory.Communication, "Agent {0}: received: {1}", AgentName, obj.Replace('\r', ' ').Replace('\n', ' '));
        }
        static void Connection_Error(Exception obj)
        {
            Logger.WriteError(LogCategory.Communication, EventId.CommunicationError, obj);
            if (_serverContext.ServerType == ServerType.Distributed)
            {
                _reconnecting = true;
            }
            else
            {
                Logger.WriteVerbose("@@@@@@@@@@@@@@@@@@@@@@ Agent {0} now commits suicide.", AgentName);
                Thread.Sleep(1000);
                Process.GetCurrentProcess().Kill();
            }
        }
        static void Connection_ConnectionSlow()
        {
            Logger.WriteWarning(LogCategory.Communication, EventId.ConnectionSlow, "Agent {0}: connection is slow.", AgentName);
        }
        static void Connection_Closed()
        {
            Logger.WriteWarning(LogCategory.Communication, EventId.AgentDisconnected, "Agent {0}: connection is closed.", AgentName);
        }

        /*----------------------------------------------------- called by hub proxy */

        private static void NewTask(SnTask t)
        {
            if (_working)
                return;
            if (t != null && !TaskExecutors.ContainsKey(t.Type))
                return;

            if (t == null)
                Logger.WriteVerbose("Agent {0} handles a 'handle-dead-tasks' message.", AgentName);
            else
                Logger.WriteVerbose("Agent {0} handles a 'new-tasks' message.", AgentName);

            Work();
        }

        /*------------------------------------------------------------------------- */

        async static void Work()
        {
            lock (_workingsync)
            {
                if (_working)
                    return;
                _working = true;
            }

            StartLockTimer();
            Console.WriteLine("_______________________________WORKING");

            try
            {
                SnTask t;
                t = await GetTask();

                while (t != null)
                {
                    _currentTask = t;
                    var result = ExecuteTask(t);
                    _currentTask = null;
                    SendResult(result);

                    // after finishing the previous one, try to get the next task
                    t = await FinishAndGetTask(t.Id);
                }
            }
            catch (Exception e)
            {
                Logger.WriteError(EventId.ExecutionError, e);
                _reconnecting = true;
            }
            finally
            {
                StopLockTimer();
                _working = false;
                Console.WriteLine("_______________________________WAITING");
            }
        }

        private static Task<SnTask> GetTask()
        {
            //return _hubProxy.Invoke<SnTask>(Hub.GetTaskMethod, AgentName);
            return InvokeProxy<SnTask>(Hub.GetTaskMethod, AgentName, _capabilities);
        }

        private static SnTaskResult ExecuteTask(SnTask t)
        {
            Console.WriteLine("Start work on task#" + t.Id);


            var result = new SnTaskResult { AgentName = AgentName, Task = t };
            try
            {
                if (t.Type == "DoNotRunAnyExecutor")
                    using (var executor = new TestExecutor())
                        result.ResultCode = executor.Execute(t);
                else
                    using (var executor = new OutProcExecutor())
                        result.ResultCode = executor.Execute(t);
            }
            catch (Exception e)
            {
                result.Exception = e;
            }
            Console.WriteLine("Execution finished.");
            return result;
        }
        private static void SendResult(SnTaskResult result)
        {
            //_hubProxy.Invoke(Hub.TaskFinished, result);
            InvokeProxy(Hub.TaskFinished, result);
        }

        private static Task<SnTask> FinishAndGetTask(int finishedTaskId)
        {
            //return _hubProxy.Invoke<SnTask>(Hub.FinishAndGetTaskMethod, AgentName, finishedTaskId);
            return InvokeProxy<SnTask>(Hub.FinishAndGetTaskMethod, AgentName, finishedTaskId, _capabilities);
        }

        // continuous lock support
        private static Timer _updateLockTimer;
        private static int _updateLockPeriodInMilliseconds;
        private static void StartLockTimer()
        {
            _updateLockTimer.Change(_updateLockPeriodInMilliseconds, _updateLockPeriodInMilliseconds);
        }
        private static void StopLockTimer()
        {
            _updateLockTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        private static void UpdateLockTimerElapsed(object o)
        {
            var taskId = _currentTask == null ? 0 : _currentTask.Id;
            if (taskId < 1)
                return;
            //_hubProxy.Invoke(Hub.RefreshLockMethod, AgentName, taskId);
            InvokeProxy(Hub.RefreshLockMethod, AgentName, taskId);
            Console.Write("*");
        }

        // watching executor support
        private static Timer _watchExecutorTimer;
        private static int _watchExecutorPeriodInMilliseconds;
        private static void StartWatcherTimer()
        {
            _watchExecutorTimer.Change(_watchExecutorPeriodInMilliseconds, _watchExecutorPeriodInMilliseconds);
        }
        private static void StopWatcherTimer()
        {
            _watchExecutorTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        private static void WatchExecutorTimerElapsed(object o)
        {
            if (DateTime.UtcNow.AddMilliseconds(-_watchExecutorPeriodInMilliseconds) > executionStateWritten)
            {
                if (_executor != null)
                    _executor.Terminate();
                Console.WriteLine("EXECUTOR TERMINATED: {0}.", _executor.Task.Type);
            }
        }
        private static IExecutor _executor = null;
        private static string progressMessage = null;
        private static DateTime executionStateWritten = DateTime.MinValue;
        internal static void ExecutionStart(IExecutor executor)
        {
            _executor = executor;
            progressMessage = null;
            executionStateWritten = DateTime.UtcNow;

            StartWatcherTimer();
        }
        internal static void ExecutionEnd()
        {
            StopWatcherTimer();

            _executor = null;
            progressMessage = null;
            executionStateWritten = DateTime.MinValue;
        }
        internal static void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e != null && e.Data != null)
            {
                if (e.Data.StartsWith("Progress:", StringComparison.OrdinalIgnoreCase))
                {
                    progressMessage = e.Data.Substring(9).Trim();
                    Logger.WriteVerbose("############ Progress received {0}: {1}", AgentName, progressMessage);
                    executionStateWritten = DateTime.UtcNow;
                    HeartBeatTimerElapsed(null);

                }
                Console.WriteLine(e.Data);
            }
        }

        // heartbeat support
        static PerformanceCounter cpuCounter = new PerformanceCounter() { CategoryName = "Processor", CounterName = "% Processor Time", InstanceName = "_Total" };
        static PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        private static bool _reconnecting;
        private static Timer _heartbeatTimer;
        private static int _heartBeatTimerPeriodInMilliseconds;
        private static void HeartBeatTimerElapsed(object o)
        {
            if (_reconnecting)
            {
                //Logger.LogMessage(string.Format("Agent {0} is trying to reconnect.", AgentName));
                Reconnect();
            }
            else
            {
                //Logger.LogMessage(string.Format("Agent {0} is calling the heartbeat method", AgentName));
                InvokeProxy(Hub.HeartbeatMethod, AgentName, GetHealthRecord());
            }
        }
        private static string GetHealthRecord()
        {
            var p = Process.GetCurrentProcess();
            var sb = new StringBuilder();

            // {MachineName: SNBPPC073, ProcessId: 14816, Progress: Idle, CPU: 0 %, RAM: 1042 MB, StartTime: 2014.06.30. 5:30:49}

            sb.Append("{");
            sb.AppendFormat("\"MachineName\": \"{0}\", ", Environment.MachineName);
            sb.AppendFormat("\"ProcessId\": \"{0}\", ", p.Id);
            sb.AppendFormat("\"TaskId\": \"{0}\", ", _currentTask == null ? 0 : _currentTask.Id);
            sb.AppendFormat("\"TaskType\": \"{0}\", ", _currentTask == null ? string.Empty : _currentTask.Type);
            sb.AppendFormat("\"Progress\": \"{0}\", ", progressMessage ?? "Idle");
            sb.AppendFormat("\"CPU\": \"{0}\", ", cpuCounter.NextValue());
            sb.AppendFormat("\"RAM\": \"{0}\", ", ramCounter.NextValue());
            sb.AppendFormat("\"TotalRAM\": \"{0}\", ", GetTotalPhysicalMemory());
            sb.AppendFormat("\"StartTime\": \"{0}\"", p.StartTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            sb.Append("}");

            return sb.ToString();
        }

        private static ulong GetTotalPhysicalMemory()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024 * 1024);
        }


        internal static void InvokeProxy(string method, params object[] args)
        {
            try
            {
                _hubProxy.Invoke(method, args);
            }
            catch (Exception e)
            {
                _reconnecting = true;
            }
        }
        private static Task<T> InvokeProxy<T>(string method, params object[] args)
        {
            try
            {
                return _hubProxy.Invoke<T>(method, args);
            }
            catch (Exception e)
            {
                _reconnecting = true;
            }
            return null;
        }
    }
}
