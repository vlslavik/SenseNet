using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using SenseNet.Diagnostics;
using System.Diagnostics;

namespace SenseNet.BackgroundOperations
{
    [SenseNetAuthorize(typeof(DistributedTaskManagerHub))]
    public class DistributedTaskManagerHub : Hub
    {
        public SnTask GetTask(string agentName, string[] capabilities)
        {
            Debug.WriteLine("#TaskManagerHub> MyHub1.GetTask called. agentName: {0}, capabilities: {1}", agentName, String.Join(", ", capabilities));

            try
            {
                var task = TaskDataHandler.GetNextAndLock(agentName, capabilities);

                Debug.WriteLine("#TaskManagerHub> TaskDataHandler.GetNextAndLock returned with: " + (task == null ? "null" : "task#" + task.Id.ToString()));
                return task;
            }
            catch (Exception e)
            {
                Debug.WriteLine("#TaskManagerHub> ERROR: " + e.Message);
            }

            return null;
        }

        public SnTask FinishAndGetTask(string agentName, int finishedTaskId, string[] capabilities)
        {
            TaskDataHandler.DeleteTask(finishedTaskId);

            Debug.WriteLine("#TaskManagerHub>FinishTask: task#{0} has been deleted.", finishedTaskId);
            return GetTask(agentName, capabilities);
        }

        public void RefreshLock(string agentName, int taskId)
        {
            TaskDataHandler.RefreshLock(taskId);
            Debug.WriteLine("#TaskManagerHub>RefreshLock: task#" + taskId + ", agent: " + agentName);
        }

        public void Heartbeat(string agentName, string healthRecord)
        {
            Debug.WriteLine(String.Format("#TaskManagerHub> HealthRecord received. Agent: {0}, data: {1}", agentName, healthRecord));
            TaskMonitorHub.Heartbeat(agentName, healthRecord);
        }

        public void TaskFinished(SnTaskResult taskResult)
        {
            Debug.WriteLine(String.Format("#TaskManagerHub> TaskFinished received. Agent: {0}, taskId: {1}, code: {2}, exception: {3}", taskResult.AgentName, taskResult.Task.Id, taskResult.ResultCode, taskResult.Exception == null ? "" : taskResult.Exception.Message));
            DistributedTaskManager.OnTaskFinished(taskResult);
        }

        public void OnEvent(TaskManagerEvent e)
        {
            TaskMonitorHub.OnEvent(e);
        }

        public ServerContext OnAgentConnected(string agentName)
        {
            return TaskManager.ServerContext;
        }

        public static int ClientCount { get { return _clientCount; } }
        private static int _clientCount;
        public override Task OnConnected()
        {
            //_connections.Add(this.Context.ConnectionId);
            _clientCount++;
            Debug.WriteLine("#TaskManagerHub> OnConnected. Clients: " + _clientCount);
            return base.OnConnected();
        }
        public override Task OnReconnected()
        {
            _clientCount++;
            Debug.WriteLine("#TaskManagerHub> OnReconnected. Clients: " + _clientCount);
            return base.OnReconnected();
        }
        public override Task OnDisconnected()
        {
            //_clientCount--;
            Debug.WriteLine("#TaskManagerHub> OnDisconnected. Clients: " + _clientCount);
            return base.OnDisconnected();
        }
        public override Task OnDisconnected(bool stopCalled)
        {
            _clientCount--;
            Debug.WriteLine("#TaskManagerHub> OnDisconnected({0}). Clients: {1}", stopCalled, _clientCount);
            return base.OnDisconnected(stopCalled);
        }
    }

    [SenseNetAuthorize(typeof(TaskMonitorHub))]
    public class TaskMonitorHub : Hub
    {
        public static void Heartbeat(string agentName, string healthRecord)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<TaskMonitorHub>();
            hubContext.Clients.All.Heartbeat(agentName, healthRecord);
        }
        public static void OnEvent(TaskManagerEvent e)
        {
            Debug.WriteLine(String.Format("#TaskManagerHub> @@@@@@@@@@ Event received. Msg: {0}, agent: {1}", e.Message, e.AgentName));
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<TaskMonitorHub>();
            hubContext.Clients.All.OnEvent(e);
        }
    }
}
