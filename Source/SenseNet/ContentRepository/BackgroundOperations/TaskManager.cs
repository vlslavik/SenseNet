using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SenseNet.BackgroundOperations
{
    public static class TaskManager
    {
        public static void RegisterTask(string type, TaskPriority priority, string data)
        {
            Instance.RegisterTask(type, priority, data);
        }

        /*==================================================================================*/

        private static ITaskFinalizer[] TaskFinalizers { get; set; }

        private static object _initializationLock = new object();
        private static ITaskManager __instance;
        private static ITaskManager Instance
        {
            get
            {
                if (__instance == null)
                {
                    lock (_initializationLock)
                    {
                        ITaskManager instance;
                        if (__instance == null)
                        {
                            if (String.IsNullOrEmpty(RepositoryConfiguration.TaskManagerClassName))
                                instance = new DefaultTaskManager();
                            else
                                instance = (ITaskManager)TypeHandler.CreateInstance(RepositoryConfiguration.TaskManagerClassName);
                            instance.TaskFinished += Instance_TaskFinished;
                            TaskFinalizers = TypeHandler.GetTypesByInterface(typeof(ITaskFinalizer))
                                .Select(t => (ITaskFinalizer)Activator.CreateInstance(t)).ToArray();
                            Logger.WriteInformation("TaskManager created.", Logger.GetDefaultProperties, instance);
                            __instance = instance;
                        }
                    }
                }
                return __instance;
            }
        }

        private static void Instance_TaskFinished(object sender, TaskFinishedEventArgs e)
        {
            Debug.WriteLine("#TaskManager> Calling finalizers (" + TaskFinalizers.Length + ")");
            foreach (var f in TaskFinalizers)
            {
                try
                {
                    f.Finalize(e.TaskResult);
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                }
            }
        }

        public static void Start()
        {
            Instance.Start();
        }
        public static void Stop()
        {
            Instance.Stop();
        }

        public static ServerContext ServerContext
        {
            get
            {
                return new ServerContext { ServerType = Instance.Distributed ? ServerType.Distributed : ServerType.Local };
            }
        }
    }

    internal class DefaultTaskManager : ITaskManager
    {
        public void Start() { }
        public void Stop() { }
        public bool Distributed { get { return false; } }
        public event EventHandler<TaskFinishedEventArgs> TaskFinished;
        public void RegisterTask(string type, TaskPriority priority, string data) { }
    }

    internal class DefaultTaskFinalizer : ITaskFinalizer
    {
        public void Finalize(SnTaskResult result)
        {
            Debug.WriteLine("#TaskManager> DefaultTaskFinalizer.Finalize called.");
        }
    }

}
