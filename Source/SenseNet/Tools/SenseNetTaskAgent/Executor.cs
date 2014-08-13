using SenseNet.BackgroundOperations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace SenseNetTaskAgent
{
    internal interface IExecutor
    {
        SnTask Task { get; }
        int Execute(SnTask task);
        void Terminate();
    }

    internal class TestExecutor : IExecutor, IDisposable
    {
        private Timer _timer = new Timer(1000.0);

        public SnTask Task { get; private set; }

        public TestExecutor()
        {
            _timer.Elapsed += _timer_Elapsed;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.Write("#");
        }

        public int Execute(SnTask task)
        {
            Task = task;

            var t = 10000 + new Random().Next(5000);
            Console.Write("t: " + t + " ");

            _timer.Start();
            System.Threading.Thread.Sleep(t);
            _timer.Stop();

            Console.WriteLine(".");
            return 0;
        }

        public void Terminate()
        {
        }
    }

    internal class OutProcExecutor : IExecutor, IDisposable
    {
        private Process _process;
        public SnTask Task { get; private set; }

        public int Execute(SnTask task)
        {
            Task = task;
            Agent.ExecutionStart(this);
            try
            {
                return ExecuteInner(task);
            }
            finally
            {
                Agent.ExecutionEnd();
            }
        }
        private int ExecuteInner(SnTask task)
        {
            var workerExe = GetWorkerExePath(task);
            if (string.IsNullOrEmpty(workerExe) || !File.Exists(workerExe))
                throw new Exception("Task executor command was not found: " + task.Type);

            var urlParameter = "REPO:\"" + Configuration.RepositoryUrl + "\"";
            var userParameter = "USERNAME:\"" + Configuration.Username + "\"";
            var passwordParameter = "PASSWORD:\"" + Configuration.Password + "\"";
            var dataParameter = "DATA:\"" + EscapeArgument(task.TaskData) + "\"";

            var prms = new List<string> { urlParameter, userParameter, passwordParameter, dataParameter };

            var processArgs = string.Join(" ", prms);
            var startInfo = new ProcessStartInfo(workerExe, processArgs)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(workerExe),
                CreateNoWindow = true,
                ErrorDialog = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            //_process = Process.Start(startInfo);
            _process = new Process();
            _process.EnableRaisingEvents = true;
            _process.StartInfo = startInfo;
            _process.OutputDataReceived += process_OutputDataReceived;

            Logger.WriteInformation(EventId.ExecutionStarted,
                "Task#{1} execution STARTED on agent {0}:\r\n    id: {1},\r\n    type: {2},\r\n    hash: {3},\r\n    order: {4},\r\n    registered: {5},\r\n    key: {6},\r\n    data: {7}"
                , Agent.AgentName, task.Id, task.Type, task.Hash, task.Order, task.RegisteredAt, task.TaskKey, task.TaskData);

            _process.Start();
            _process.BeginOutputReadLine();
            _process.WaitForExit();

            _process.OutputDataReceived -= process_OutputDataReceived;
            var result = _process.ExitCode;

            if (result != 0)
                Logger.WriteWarning(EventId.TaskErrorResult, "Task#{0} execution TERMINATED with error. Result:{1}, task type: {2}, agent: {3}", task.Id, result, task.Type, Agent.AgentName);
            else
                Logger.WriteInformation(EventId.ExecutionFinished, "Task#{0} execution FINISHED: type: {1}, agent: {2}", task.Id, task.Type, Agent.AgentName);

            _process.Dispose();
            _process = null;
            return result;
        }

        public void Terminate()
        {
            if (_process != null && !_process.HasExited)
                _process.Kill();
        }

        void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Agent.OutputDataReceived(sender, e);
        }

        private string GetWorkerExePath(SnTask task)
        {
            string exePath;
            if (Agent.TaskExecutors.TryGetValue(task.Type, out exePath))
                return exePath;
            return null;
        }

        private static string EscapeArgument(string arg)
        {
            return arg.Replace("\"", "\"\"");
        }

        #region IDisposable Pattern
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~OutProcExecutor()
        {
            Dispose(false);
        }
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here.
                    if (_process != null && !_process.HasExited)
                    {
                        _process.Kill();
                        _process.Dispose();
                    }
                }
                // Clean up unmanaged resources here.
            }
            _disposed = true;
        }
        #endregion

    }
}
