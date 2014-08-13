using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.BackgroundOperations
{
    internal abstract class TaskManagerBase : ITaskManager
    {
        public abstract bool Distributed { get; }
        public abstract void Start();
        public abstract void Stop();

        public event EventHandler<TaskFinishedEventArgs> TaskFinished;

        private static TaskManagerBase _instance;

        int _handleDeadTaskPeriodInMilliseconds = 60 * 1000;
        private Timer _deadTaskTimer;

        public TaskManagerBase()
        {
            _deadTaskTimer = new Timer(new TimerCallback(DeadTaskTimerElapsed), null, _handleDeadTaskPeriodInMilliseconds, _handleDeadTaskPeriodInMilliseconds);
            TaskManagerBase._instance = this;
            Debug.WriteLine("#TaskManager> TaskManagerBase._instance assigned.");
        }

        public void RegisterTask(string type, TaskPriority priority, string data)
        {
            var task = TaskDataHandler.RegisterTask(type, priority, data);
            if (task != null)
                BroadcastMessage(task);
        }
        private void BroadcastMessage(SnTask task)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<DistributedTaskManagerHub>();
            hubContext.Clients.All.NewTask(task);
        }

        private void DeadTaskTimerElapsed(object o)
        {
            if (TaskDataHandler.GetDeadTaskCount() > 0)
                BroadcastMessage(null);
        }

        internal static void OnTaskFinished(SnTaskResult taskResult)
        {
            Debug.WriteLine("#TaskManager> TaskManagerBase.OnTaskFinished fired.");
            if (_instance.TaskFinished != null)
                _instance.TaskFinished(_instance, new TaskFinishedEventArgs { TaskResult = taskResult });
        }
    }
}
