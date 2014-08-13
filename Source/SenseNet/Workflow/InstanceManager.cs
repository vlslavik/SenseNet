using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.Activities.DurableInstancing;
using System.Xml.Linq;
using System.Runtime.DurableInstancing;
using SenseNet.Diagnostics;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using System.Reflection;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository;
using SenseNet.Search;
//using System.Configuration;

namespace SenseNet.Workflow
{
    public enum WorkflowApplicationCreationPurpose { StartNew, Resume, Poll, Abort };
    public enum WorkflowApplicationAbortReason { ManuallyAborted, StateContentDeleted, RelatedContentChanged, RelatedContentDeleted };

    public static class InstanceManager
    {
        private const string STATECONTENT = "StateContent";
        private const double MINPOLLINTERVAL = 2000.0;

        public static void StartWorkflowSystem()
        {
            WriteDebug("Start Workflow System");
        }

        //=========================================================================================================== Polling

        static System.Timers.Timer _pollTimer;
        static InstanceManager()
        {
            var pollInterval = Configuration.TimerInterval * 60.0 * 1000.0;

            if (pollInterval >= MINPOLLINTERVAL)
            {
                _pollTimer = new System.Timers.Timer(pollInterval);
                _pollTimer.Elapsed += new System.Timers.ElapsedEventHandler(PollTimerElapsed);
                _pollTimer.Disposed += new EventHandler(PollTimerDisposed);
                _pollTimer.Enabled = true;
                Logger.WriteInformation(Logger.EventId.NotDefined, "Starting polling timer. Interval in minutes: " + Configuration.TimerInterval);
            }
            else
            {
                Logger.WriteWarning(Logger.EventId.NotDefined, String.Format("Polling timer was not started because the configured interval ({0}) is less than acceptable minimum ({1}). Interval in minutes: ",
                    Configuration.TimerInterval, MINPOLLINTERVAL));
            }
        }
        private static void PollTimerDisposed(object sender, EventArgs e)
        {
            _pollTimer.Elapsed -= new System.Timers.ElapsedEventHandler(PollTimerElapsed);
            _pollTimer.Disposed -= new EventHandler(PollTimerDisposed);
        }
        private static void PollTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            WriteDebug(">>>PollTimerElapsed");

            var msg = new StringBuilder();
            int counter = 0;
            _pollTimer.Enabled = false;
            try
            {
                ManageOrphanedLockOwners();

                foreach (var item in GetPollingInstances())
                {
                    try
                    {
                        ExecuteDelays(item);
                    }
                    catch (Exception ex)
                    {
                        if (msg.Length == 0)
                            msg.AppendLine("##WF> Errors:");
                        msg.AppendFormat("{0}.: {1} was thrown during processing {2}. Message: {3}{4}", ++counter, ex.GetType().FullName, item.Path, ex.Message, Environment.NewLine);
                    }
                }
                if (msg.Length > 0)
                    throw new ApplicationException(msg.ToString());
            }
            finally
            {
                _pollTimer.Enabled = true;
            }

            WriteDebug("<<<PollTimerElapsed");
        }
        public static IEnumerable<WorkflowHandlerBase> GetPollingInstances()
        {
            try
            {
                var result = SenseNet.Search.ContentQuery.Query(SafeQueries.GetPollingInstances, null, WorkflowStatusEnum.Running);
                var instances = new Dictionary<string, WorkflowHandlerBase>();
                foreach (WorkflowHandlerBase item in result.Nodes)
                {
                    var key = String.Format("{0}-{1}", item.WorkflowTypeName, item.WorkflowDefinitionVersion);
                    if (!instances.ContainsKey(key))
                        instances.Add(key, item);
                }
                Debug.WriteLine("##WF> Trying execute active workflows. ResultCount: " + result.Count + ", PollingItems: " + instances.Count);
                Logger.WriteVerbose("Trying execute active workflows", null,
                    new Dictionary<string, object> { { "ResultCount", result.Count }, { "PollingItems", instances.Count }, });

                return instances.Values.ToArray();
            }
            catch (Exception e)
            {
                WriteError("GetPollingInstances", e);
                throw;
            }
        }
        private static void _Poll()
        {
            try
            {
                foreach (var item in GetPollingInstances())
                    ExecuteDelays(item);
            }
            catch (Exception e)
            {
                WriteError("_Poll", e);
                throw;
            }
        }

        //=========================================================================================================== Building

        private static string ConnectionString { get { return RepositoryConfiguration.ConnectionString; } }
        private static WorkflowDataClassesDataContext GetDataContext()
        {
            try
            {
                return new WorkflowDataClassesDataContext(ConnectionString);
            }
            catch (Exception e)
            {
                WriteError("GetDataContext", e);
                throw;
            }
        }

        private static WorkflowApplication CreateWorkflowApplication(WorkflowHandlerBase workflowInstance, WorkflowApplicationCreationPurpose purpose, IDictionary<string, object> parameters)
        {
            try
            {
                string version;
                WorkflowApplication wfApp = null;
                var workflow = workflowInstance.CreateWorkflowInstance(out version);
                switch (purpose)
                {
                    case WorkflowApplicationCreationPurpose.StartNew:
                        Dictionary<string, object> arguments = workflowInstance.CreateParameters();
                        arguments.Add(STATECONTENT, new WfContent(workflowInstance));
                        if (parameters != null)
                            foreach (var item in parameters)
                                arguments.Add(item.Key, item.Value);
                        wfApp = new WorkflowApplication(workflow, arguments);
                        workflowInstance.WorkflowDefinitionVersion = version;
                        workflowInstance.WorkflowInstanceGuid = wfApp.Id.ToString();
                        break;
                    default:
                        wfApp = new WorkflowApplication(workflow);
                        break;
                }

                WriteDebug("CreateWorkflowApplication: NodeId: " + workflowInstance.Id + ", instanceId: " + workflowInstance.WorkflowInstanceGuid + ", Purpose: " + purpose);

                InstanceHandle ownerHandle;
                var store = CreateInstanceStore(workflowInstance, out ownerHandle);
                Dictionary<XName, object> wfScope = new Dictionary<XName, object> { { GetWorkflowHostTypePropertyName(), GetWorkflowHostTypeName(workflowInstance) } };

                wfApp.InstanceStore = store;
                wfApp.AddInitialInstanceValues(wfScope);

                wfApp.PersistableIdle = a => { WriteDebug("PersistableIdle " + wfApp.Id); DestroyInstanceOwner(wfApp, ownerHandle); return PersistableIdleAction.Unload; };
                wfApp.Unloaded = b => { WriteDebug("Unloaded " + wfApp.Id); DestroyInstanceOwner(wfApp, ownerHandle); };
                wfApp.Completed = c => { WriteDebug("Completed " + wfApp.Id); OnWorkflowCompleted(c); DestroyInstanceOwner(wfApp, ownerHandle); };
                wfApp.Aborted = d => { WriteDebug("Aborted " + wfApp.Id); OnWorkflowAborted(d); DestroyInstanceOwner(wfApp, ownerHandle); };
                wfApp.OnUnhandledException = e => { WriteDebug("OnUnhandledException " + wfApp.Id); return HandleError(e); };

                wfApp.Extensions.Add(new ContentWorkflowExtension() { WorkflowInstancePath = workflowInstance.Path });
                return wfApp;
            }
            catch (Exception e)
            {
                WriteError("CreateWorkflowApplication", e);
                throw;
            }
        }
        private static SqlWorkflowInstanceStore CreateInstanceStore(WorkflowHandlerBase workflowInstance, out InstanceHandle ownerHandle)
        {
            try
            {
                //WriteDebug("CreateInstanceStore: " + workflowInstance.WorkflowInstanceGuid + ", nodeId: " + workflowInstance.Id);

                var store = new SqlWorkflowInstanceStore(ConnectionString);
                ownerHandle = store.CreateInstanceHandle();

                var wfHostTypeName = GetWorkflowHostTypeName(workflowInstance);
                var WorkflowHostTypePropertyName = GetWorkflowHostTypePropertyName();

                var ownerCommand = new CreateWorkflowOwnerCommand() { InstanceOwnerMetadata = { { WorkflowHostTypePropertyName, new InstanceValue(wfHostTypeName) } } };
                var owner = store.Execute(ownerHandle, ownerCommand, TimeSpan.FromSeconds(30)).InstanceOwner;
                ownerHandle.Free();
                store.DefaultInstanceOwner = owner;
                return store;
            }
            catch (Exception e)
            {
                WriteError("CreateInstanceStore", e);
                throw;
            }
        }
        private static void DestroyInstanceOwner(WorkflowApplication wfApp, InstanceHandle instanceHandle)
        {
            try
            {
                if (instanceHandle.IsValid)
                {
                    //WriteDebug("DestroyInstanceOwner: " + wfApp.Id);
                    var deleteOwnerCmd = new DeleteWorkflowOwnerCommand();
                    wfApp.InstanceStore.Execute(instanceHandle, deleteOwnerCmd, TimeSpan.FromSeconds(30));
                    wfApp.InstanceStore.DefaultInstanceOwner = null;
                }
                else
                {
                    //WriteDebug("DestroyInstanceOwner: HANDLE IS FREED: " + wfApp.Id);
                }
            }
            catch (Exception e)
            {
                WriteError("DestroyInstanceOwner", e);
                throw;
            }
        }

        private static XName GetWorkflowHostTypePropertyName()
        {
            return XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties").GetName("WorkflowHostType");
        }
        private static XName GetWorkflowHostTypeName(WorkflowHandlerBase workflowInstance)
        {
            return XName.Get(workflowInstance.WorkflowHostType, "http://www.sensenet.com/2010/workflow");
        }

        //=========================================================================================================== Operations

        public static Guid Start(WorkflowHandlerBase workflowInstance)
        {
            try
            {
                var wfApp = CreateWorkflowApplication(workflowInstance, WorkflowApplicationCreationPurpose.StartNew, null);
                var id = wfApp.Id;
                workflowInstance.WorkflowStatus = WorkflowStatusEnum.Running;
                workflowInstance.DisableObserver(typeof(WorkflowNotificationObserver));
                using (new SystemAccount())
                    workflowInstance.Save();
                wfApp.Run();
                return id;
            }
            catch (Exception e)
            {
                WriteError("Start", e);
                throw;
            }
        }

        public static void Abort(WorkflowHandlerBase workflowInstance, WorkflowApplicationAbortReason reason)
        {
            try
            {
                //check permissions
                if (reason == WorkflowApplicationAbortReason.ManuallyAborted && !workflowInstance.Security.HasPermission(PermissionType.Save))
                {
                    Logger.WriteVerbose(String.Concat("InstanceManager cannot abort the instance: ", workflowInstance.Path, ", because the user doesn't have the sufficient permissions (Save)."));
                    throw new SenseNetSecurityException(workflowInstance.Path, PermissionType.Save, AccessProvider.Current.GetCurrentUser());
                }

                //abort the workflow
                var name = workflowInstance == null ? "[null]" : workflowInstance.Name;
                try
                {
                    var wfApp = CreateWorkflowApplication(workflowInstance, WorkflowApplicationCreationPurpose.Abort, null);

                    wfApp.Load(Guid.Parse(workflowInstance.WorkflowInstanceGuid));
                    //wfApp.Abort();
                    wfApp.Cancel(); //.Terminate(string.Format("#### Aborted. Reason {0}, Path: {1}", reason, workflowInstance.Path));
                }
                catch (Exception e)
                {
                    WriteDebug("================ CANNOT ABORT: " + name);
                    Logger.WriteVerbose(String.Concat("InstanceManager cannot abort the instance: ", workflowInstance.Path, ". Exception message: ", e.Message));
                }

                //write back workflow state
                WriteBackAbortMessage(workflowInstance, reason);
            }
            catch (Exception e)
            {
                WriteError("Abort", e);
                throw;
            }
        }

        public static void ExecuteDelays(WorkflowHandlerBase workflowInstance)
        {
            //var abortedList = new List<Guid>();
            //var doneList = new List<Guid>();

            //while (true)
            //{
            //    var wfApp = CreateWorkflowApplication(workflowInstance, WorkflowApplicationCreationPurpose.Poll, null);
            //    try
            //    {
            //        wfApp.LoadRunnableInstance(TimeSpan.FromSeconds(1));
            //        if (doneList.Contains(wfApp.Id))
            //        {
            //            wfApp.Cancel();
            //            WriteBackAbortMessage(workflowInstance, "Cannot execute the workflow twice in the same period. The workflow may contain a corrupt operation (e.g. state or related content modification).");
            //        }
            //        else
            //        {
            //            if (ValidWakedUpWorkflow(wfApp))
            //            {
            //                wfApp.Run();
            //                doneList.Add(wfApp.Id);
            //            }
            //            else
            //            {
            //                if (!abortedList.Contains(wfApp.Id))
            //                {
            //                    abortedList.Add(wfApp.Id);
            //                    wfApp.Cancel();
            //                }
            //            }
            //        }
            //    }
            //    catch (InstanceNotReadyException)
            //    {
            //        break;
            //    }
            //}

            try
            {
                var wfApps = LoadRunnableInstances(workflowInstance);
                foreach (var wfApp in wfApps)
                {
                    if (ValidWakedUpWorkflow(wfApp))
                        wfApp.Run();
                    else
                        wfApp.Cancel();
                }
            }
            catch (Exception e)
            {
                WriteError("ExecuteDelays", e);
                throw;
            }
        }

        private static IEnumerable<WorkflowApplication> LoadRunnableInstances(WorkflowHandlerBase workflowInstance)
        {
            try
            {
                var wfApps = new List<WorkflowApplication>();
                while (true)
                {
                    try
                    {
                        var wfApp = CreateWorkflowApplication(workflowInstance, WorkflowApplicationCreationPurpose.Poll, null);
                        wfApp.LoadRunnableInstance();
                        wfApps.Add(wfApp);
                    }
                    catch (InstanceNotReadyException)
                    {
                        break;
                    }
                }

                var loadedInstanceIds = wfApps.Select(w => w.Id).ToArray();
                WriteDebug("<#> Loaded instances: (" + loadedInstanceIds.Length + "): " + String.Join(", ", loadedInstanceIds));

                return wfApps;
            }
            catch (Exception e)
            {
                WriteError("LoadRunnableInstances", e);
                throw;
            }
        }

        public static void FireNotification(WorkflowNotification notification, WorkflowNotificationEventArgs eventArgs)
        {
            try
            {
                var wfInstance = Node.Load<WorkflowHandlerBase>(notification.WorkflowNodePath);
                var wfApp = CreateWorkflowApplication(wfInstance, WorkflowApplicationCreationPurpose.Resume, null);
                wfApp.Load(notification.WorkflowInstanceId);
                //wfApp.ResumeBookmark(notification.BookmarkName, notification.NodeId);
                if (ValidWakedUpWorkflow(wfApp))
                {
                    Debug.WriteLine(String.Format("##WF> FireNotification: ResumeBookmark: {0}|{1}", notification.NodeId, notification.WorkflowInstanceId));
                    wfApp.ResumeBookmark(notification.BookmarkName, eventArgs);
                }
                else
                {
                    Debug.WriteLine(String.Format("##WF> FireNotification: Cancel: {0}|{1}", notification.NodeId, notification.WorkflowInstanceId));
                    wfApp.Cancel();
                }
            }
            catch (Exception e)
            {
                WriteError("FireNotification", e);
                throw;
            }
        }
        public static void NotifyContentChanged(WorkflowNotificationEventArgs eventArgs)
        {
            try
            {
                WorkflowNotification[] notifications = null;
                using (var dbContext = GetDataContext())
                {
                    notifications = dbContext.WorkflowNotifications.Where(notification =>
                        notification.NodeId == eventArgs.NodeId).ToArray();
                }

                //Debug.WriteLine(String.Format("##WF> NotifyContentChanged: Loaded notifications {0}: {1}", notifications.Length, String.Join(", ", notifications.Select(n => n.NodeId + "|" + n.WorkflowInstanceId))));

                foreach (var notification in notifications)
                    InstanceManager.FireNotification(notification, eventArgs);
            }
            catch (Exception e)
            {
                WriteError("NotifyContentChanged", e);
                throw;
            }
        }
        public static int RegisterWait(int nodeID, Guid wfInstanceId, string bookMarkName, string wfContentPath)
        {
            try
            {
                WriteDebug("**************** RegisterWait: " + wfInstanceId);
                using (var dbContext = GetDataContext())
                {
                    var notification = new WorkflowNotification()
                    {
                        BookmarkName = bookMarkName,
                        NodeId = nodeID,
                        WorkflowInstanceId = wfInstanceId,
                        WorkflowNodePath = wfContentPath
                    };
                    dbContext.WorkflowNotifications.InsertOnSubmit(notification);
                    dbContext.SubmitChanges();
                    return notification.NotificationId;
                }
            }
            catch (Exception e)
            {
                WriteError("RegisterWait", e);
                throw;
            }
        }
        public static void ReleaseWait(int notificationId)
        {
            try
            {
                WriteDebug("**************** RegisterWait: " + notificationId);
                using (var dbContext = GetDataContext())
                {
                    var ent = dbContext.WorkflowNotifications.SingleOrDefault(wn => wn.NotificationId == notificationId);
                    dbContext.WorkflowNotifications.DeleteOnSubmit(ent);
                    dbContext.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                WriteError("ReleaseWait", e);
                throw;
            }
        }

        //=========================================================================================================== Events

        private static void OnWorkflowAborted(WorkflowApplicationAbortedEventArgs args)
        {
            try
            {
                DeleteNotifications(args.InstanceId);
                WriteBackTheState(WorkflowStatusEnum.Aborted, args.InstanceId);

                // also write back abort message, if it is not yet given
                var stateContent = GetStateContent(args.InstanceId);
                if (stateContent == null)
                    return;

                WriteBackAbortMessage(stateContent, DumpException(args.Reason));
            }
            catch (Exception e)
            {
                WriteError("OnWorkflowAborted", e);
                throw;
            }
        }
        private static void OnWorkflowCompleted(WorkflowApplicationCompletedEventArgs args)
        {
            try
            {
                DeleteNotifications(args.InstanceId);
                WriteBackTheState(WorkflowStatusEnum.Completed, args.InstanceId);
            }
            catch (Exception e)
            {
                WriteError("OnWorkflowCompleted", e);
                throw;
            }
        }
        private static void DeleteNotifications(Guid instanceId)
        {
            try
            {
                using (var dbContext = GetDataContext())
                {
                    var notifications = dbContext.WorkflowNotifications.Where(notification =>
                        notification.WorkflowInstanceId == instanceId);

                    dbContext.WorkflowNotifications.DeleteAllOnSubmit(notifications);
                    dbContext.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                WriteError("DeleteNotifications", e);
                throw;
            }
        }

        private static UnhandledExceptionAction HandleError(WorkflowApplicationUnhandledExceptionEventArgs args)
        {
            try
            {
                Logger.WriteException(args.UnhandledException);

                WorkflowHandlerBase stateContent = GetStateContent(args);
                if (stateContent == null)
                    Logger.WriteWarning(Logger.EventId.NotDefined, "The workflow InstanceManager cannot write back the aborting/terminating reason into the workflow state content.");
                else
                    WriteBackAbortMessage(stateContent, DumpException(args));
            }
            catch (Exception e)
            {
                Debug.WriteLine("##WF> OnUnhandledException: " + e.ToString().Replace("\r\n", "<br/>").Replace("\r", "<br/>").Replace("\n", "<br/>"));
                Logger.WriteException(e);
            }
            return UnhandledExceptionAction.Abort;
        }

        //=========================================================================================================== Tools

        private static bool ValidWakedUpWorkflow(WorkflowApplication wfApp)
        {
            try
            {
                var stateContent = GetStateContent(wfApp.Id);
                if (stateContent == null)
                {
                    WriteBackAbortMessage(null, WorkflowApplicationAbortReason.StateContentDeleted);
                    return false;
                }

                if (!stateContent.ContentWorkflow)
                    return true;

                if (stateContent.RelatedContent == null)
                {
                    WriteBackAbortMessage(stateContent, WorkflowApplicationAbortReason.RelatedContentDeleted);
                    return false;
                }
                if (stateContent.RelatedContentTimestamp != stateContent.RelatedContent.NodeTimestamp)
                {
                    WriteBackAbortMessage(stateContent, WorkflowApplicationAbortReason.RelatedContentChanged);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                WriteError("ValidWakedUpWorkflow", e);
                throw;
            }
        }

        private const string ABORTEDBYUSERMESSAGE = "Aborted manually by the following user: ";
        private static string GetAbortMessage(WorkflowApplicationAbortReason reason, WorkflowHandlerBase workflow)
        {
            try
            {
                switch (reason)
                {
                    case WorkflowApplicationAbortReason.ManuallyAborted:
                        return String.Concat(ABORTEDBYUSERMESSAGE, AccessProvider.Current.GetCurrentUser().Username);
                    case WorkflowApplicationAbortReason.StateContentDeleted:
                        return "Workflow deleted" + (workflow == null ? "." : (": " + workflow.Path));
                    case WorkflowApplicationAbortReason.RelatedContentChanged:
                        return "Aborted because the related content was changed.";
                    case WorkflowApplicationAbortReason.RelatedContentDeleted:
                        return "Aborted because the related content was moved or deleted.";
                    default:
                        return reason.ToString();
                }
            }
            catch (Exception e)
            {
                WriteError("GetAbortMessage", e);
                throw;
            }
        }
        private static void WriteBackAbortMessage(WorkflowHandlerBase stateContent, WorkflowApplicationAbortReason reason)
        {
            try
            {
                var abortMessage = GetAbortMessage(reason, stateContent);
                if (reason == WorkflowApplicationAbortReason.StateContentDeleted)
                {
                    var msg = "Workflow aborted. Reason: " + abortMessage + ".";
                    if (stateContent != null)
                        msg += " InstanceId: " + stateContent.WorkflowInstanceGuid;
                    Debug.WriteLine("##WF> " + msg);
                    Logger.WriteInformation(Logger.EventId.NotDefined, msg);
                }
                else
                    WriteBackAbortMessage(stateContent, abortMessage);
            }
            catch (Exception e)
            {
                WriteError("WriteBackAbortMessage#1", e);
                throw;
            }
        }
        private static void WriteBackAbortMessage(WorkflowHandlerBase stateContent, string abortMessage)
        {
            try
            {
                var state = stateContent.WorkflowStatus;
                if (state == WorkflowStatusEnum.Completed)
                    return;

                // if a system message has already been persisted to the workflow content, don't overwrite it
                if (!string.IsNullOrEmpty(stateContent.SystemMessages))
                    return;

                var times = 3;
                while (true)
                {
                    try
                    {
                        stateContent.SystemMessages = abortMessage;
                        stateContent.DisableObserver(typeof(WorkflowNotificationObserver));
                        Debug.WriteLine("##WF> Workflow aborted. Reason: " + abortMessage + ". InstanceId: " + stateContent.WorkflowInstanceGuid);
                        using (new SystemAccount())
                            stateContent.Save(SenseNet.ContentRepository.SavingMode.KeepVersion);
                        break;
                    }
                    catch (NodeIsOutOfDateException ne)
                    {
                        if (--times == 0)
                            throw new NodeIsOutOfDateException("Node is out of date after 3 trying", ne);
                        var msg = "InstanceManager: Saving system message caused NodeIsOutOfDateException. Trying again.";
                        Logger.WriteVerbose(msg);
                        Debug.WriteLine("##WF> ERROR " + msg);
                        stateContent = (WorkflowHandlerBase)Node.LoadNodeByVersionId(stateContent.VersionId);
                    }
                    catch (Exception e)
                    {
                        var msg = String.Format("InstanceManager:  Cannot write back a system message to the workflow state content. InstanceId: {0}. Path: {1}. Message: {2}. InstanceId: {3}."
                           , stateContent.Id, stateContent.Path, abortMessage, stateContent.WorkflowInstanceGuid);
                        Debug.WriteLine("##WF> ERROR " + msg);
                        Logger.WriteWarning(Logger.EventId.NotDefined, msg, properties: new Dictionary<string, object> { { "Exception", e } });
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                WriteError("WriteBackAbortMessage#2", e);
                throw;
            }
        }

        private static void WriteBackTheState(WorkflowStatusEnum state, Guid instanceId)
        {
            try
            {
                var stateContent = GetStateContent(instanceId);
                if (stateContent == null)
                    return;

                switch (stateContent.WorkflowStatus)
                {
                    case WorkflowStatusEnum.Created:
                        if (state == WorkflowStatusEnum.Created)
                            return;
                        break;
                    case WorkflowStatusEnum.Running:
                        if (state == WorkflowStatusEnum.Created || state == WorkflowStatusEnum.Running)
                            return;
                        break;
                    case WorkflowStatusEnum.Aborted:
                    case WorkflowStatusEnum.Completed:
                        return;
                    default:
                        break;
                }

                var times = 3;
                while (true)
                {
                    try
                    {
                        stateContent.WorkflowStatus = state;
                        stateContent.DisableObserver(typeof(WorkflowNotificationObserver));
                        using (new SystemAccount())
                            stateContent.Save(SenseNet.ContentRepository.SavingMode.KeepVersion);
                        //Debug.WriteLine(String.Format("##WF> InstanceManager: WriteBackTheState: {0}, id: {1}, path: {2}", state, instanceId, stateContent.Path));
                        break;
                    }
                    catch (NodeIsOutOfDateException ne)
                    {
                        if (--times == 0)
                            throw new NodeIsOutOfDateException("Node is out of date after 3 trying", ne);
                        var msg = "InstanceManager: Writing back the workflow state caused NodeIsOutOfDateException. Trying again";
                        Debug.WriteLine("##WF> " + msg);
                        Logger.WriteVerbose(msg);
                        stateContent = (WorkflowHandlerBase)Node.LoadNodeByVersionId(stateContent.VersionId);
                    }
                    catch (Exception e)
                    {
                        var msg = String.Format("Workflow state is {0} but cannot write back to the workflow state content. InstanceId: {1}. Path: {2}"
                           , state, instanceId, stateContent.Path);
                        Debug.WriteLine("##WF> ERROR" + msg);
                        Logger.WriteWarning(Logger.EventId.NotDefined, msg, properties: new Dictionary<string, object> { { "Exception", e } });
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                WriteError("WriteBackTheState", e);
                throw;
            }
        }

        private static string DumpException(WorkflowApplicationUnhandledExceptionEventArgs args)
        {
            try
            {
                var e = args.UnhandledException;
                var sb = new StringBuilder();
                sb.AppendLine("An unhandled exception occurred during the workflow execution. Please review the following information.<br />");
                sb.AppendLine();
                sb.Append("Workflow instance: ").Append(args.InstanceId.ToString()).AppendLine("<br />");
                sb.AppendFormat("Source activity: {0} ({1}, {2})", args.ExceptionSource.DisplayName, args.ExceptionSource.GetType().FullName, args.ExceptionSource.Id);
                sb.AppendLine("<br />");
                sb.AppendLine("<br />");

                sb.Append(DumpException(e));

                return sb.ToString();
            }
            catch (Exception e)
            {
                WriteError("DumpException#1", e);
                throw;
            }
        }
        private static string DumpException(Exception ee)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("========== Exception:").AppendLine("<br />");
                sb.Append(ee.GetType().Name).Append(":").Append(ee.Message).AppendLine("<br />");
                DumpTypeLoadError(ee as ReflectionTypeLoadException, sb);
                sb.Append(ee.StackTrace).AppendLine("<br />");
                while ((ee = ee.InnerException) != null)
                {
                    sb.Append("---- Inner Exception:").AppendLine("<br />");
                    sb.Append(ee.GetType().Name).Append(": ").Append(ee.Message).AppendLine("<br />");
                    DumpTypeLoadError(ee as ReflectionTypeLoadException, sb);
                    sb.Append(ee.StackTrace).AppendLine("<br />");
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                WriteError("DumpException#2", e);
                throw;
            }
        }
        private static void DumpTypeLoadError(ReflectionTypeLoadException exc, StringBuilder sb)
        {
            try
            {
                if (exc == null)
                    return;
                sb.Append("LoaderExceptions:").AppendLine("<br />");
                foreach (var e in exc.LoaderExceptions)
                {
                    sb.Append("-- ");
                    sb.Append(e.GetType().FullName);
                    sb.Append(": ");
                    sb.Append(e.Message).AppendLine("<br />");

                    var fileNotFoundException = e as System.IO.FileNotFoundException;
                    if (fileNotFoundException != null)
                    {
                        sb.Append("FUSION LOG:").AppendLine("<br />");
                        sb.Append(fileNotFoundException.FusionLog).AppendLine("<br />");
                    }
                }
            }
            catch (Exception e)
            {
                WriteError("DumpTypeLoadError", e);
                throw;
            }
        }

        private static WorkflowHandlerBase GetStateContent(WorkflowApplicationUnhandledExceptionEventArgs args)
        {
            try
            {
                WorkflowHandlerBase stateContent = null;
                var exts = args.GetInstanceExtensions<ContentWorkflowExtension>();
                if (exts != null)
                {
                    var ext = exts.FirstOrDefault();
                    if (ext != null)
                        stateContent = Node.Load<WorkflowHandlerBase>(ext.WorkflowInstancePath);
                }
                return stateContent;
            }
            catch (Exception e)
            {
                WriteError("GetStateContent#1", e);
                throw;
            }
        }
        private static WorkflowHandlerBase GetStateContent(Guid instanceId)
        {
            try
            {
                var stateContent = (WorkflowHandlerBase)SenseNet.Search.ContentQuery.Query(SafeQueries.WorkflowStateContent,
                    null, WorkflowHandlerBase.WORKFLOWINSTANCEGUID, instanceId).Nodes.FirstOrDefault();
                return stateContent;
            }
            catch (Exception e)
            {
                WriteError("GetStateContent#2", e);
                throw;
            }
        }

        private static void WriteDebug(object msg)
        {
            Debug.WriteLine("##WF> " + msg);
        }
        private static void WriteError(string methodName, Exception e)
        {
            WriteDebug("ERROR: " + methodName + ": " + e.Message);
            foreach (var line in e.StackTrace.Split('\n', '\r'))
                if (line.Length > 0)
                    WriteDebug("........" + line);
            var ee = e.InnerException;
            while (ee != null)
            {
                WriteDebug("....Inner error: " + ee.Message);
                foreach (var line in ee.StackTrace.Split('\n', '\r'))
                    if (line.Length > 0)
                        WriteDebug("...." + line);
                ee = ee.InnerException;
            }
        }

        private static Dictionary<Guid, int> lockOwners = new Dictionary<Guid, int>();
        private static int LOCKOWNERMAXLOADEDGENERATION = 3;
        private static void ManageOrphanedLockOwners()
        {
            try
            {
                var loaded = LoadOrphanedLockOwners();

                var keysToRemove = lockOwners.Keys.Except(loaded).ToArray();
                foreach (var key in keysToRemove)
                    lockOwners.Remove(key);

                var keysToDelete = new List<Guid>();
                foreach (var id in loaded)
                {
                    int gen;
                    if (lockOwners.TryGetValue(id, out gen))
                    {
                        gen++;
                        if (gen >= LOCKOWNERMAXLOADEDGENERATION)
                        {
                            keysToDelete.Add(id);
                            lockOwners.Remove(id);
                        }
                        else
                        {
                            lockOwners[id] = gen;
                        }
                    }
                    else
                    {
                        lockOwners.Add(id, 1);
                    }
                }
                DeleteOrphanedLockOwners(keysToDelete);

//for (var i = 1; i < LOCKOWNERMAXLOADEDGENERATION; i++)
//{
//    var gen = lockOwners.Where(x => x.Value == i).Select(x => x.Key).ToArray();
//    WriteDebug("########    Gen" + i + " lock owners (" + gen.Length + "):" + string.Join(", ", gen));
//}
//WriteDebug("######## deleted lock owners (" + keysToDelete.Count + "):" + string.Join(", ", keysToDelete));

            }
            catch (Exception e)
            {
                WriteError("ManageOrphanedLockOwners", e);
            }
        }
        private static Guid[] LoadOrphanedLockOwners()
        {
            var result = new List<Guid>();
            var query = @"
SELECT L.Id FROM [System.Activities.DurableInstancing].[LockOwnersTable] L
	LEFT OUTER JOIN [System.Activities.DurableInstancing].[InstancesTable] I ON L.SurrogateLockOwnerId = I.SurrogateLockOwnerId
WHERE I.Id IS NULL";

            using (var cn = new System.Data.SqlClient.SqlConnection(ConnectionString))
            using (var cm = new System.Data.SqlClient.SqlCommand(query, cn) { CommandType = System.Data.CommandType.Text })
            {
                cn.Open();
                using (var reader = cm.ExecuteReader())
                {
                    while (reader.Read())
                        result.Add(reader.GetGuid(0));
                }
            }
            return result.ToArray();
        }
        private static void DeleteOrphanedLockOwners(List<Guid> toDelete)
        {
            if (toDelete.Count == 0)
                return;

            var sql = new StringBuilder();
            foreach (var id in toDelete)
                sql.AppendFormat("DELETE FROM [System.Activities.DurableInstancing].[LockOwnersTable] WHERE Id = '{0}'", id).AppendLine();

            using (var cn = new System.Data.SqlClient.SqlConnection(ConnectionString))
            using (var cm = new System.Data.SqlClient.SqlCommand(sql.ToString(), cn) { CommandType = System.Data.CommandType.Text })
            {
                cn.Open();
                var result = cm.ExecuteNonQuery();
            }
            WriteDebug("Deleted orphaned lock owners: " + toDelete.Count);
        }

        //=========================================================================================================== RelatedContentProtector

        internal static IDisposable CreateRelatedContentProtector(Node node, ActivityContext context)
        {
            return new RelatedContentProtector(node, context);
        }
        private class RelatedContentProtector : IDisposable
        {
            private Node _node;
            private ActivityContext _context;
            public RelatedContentProtector(Node node, ActivityContext context)
            {
                //Debug.WriteLine("##WF> RelatedContentProtector instatiating: " + node.Path);
                this._node = node;
                this._context = context;
                node.DisableObserver(typeof(WorkflowNotificationObserver));
            }
            private void Release()
            {
                //Debug.WriteLine("##WF> RelatedContentProtector releasing: " + _node.Path);
                var path = _context.GetExtension<ContentWorkflowExtension>().WorkflowInstancePath;
                var stateContent = Node.Load<WorkflowHandlerBase>(path);
                if (stateContent.RelatedContent.Id == _node.Id)
                {
                    stateContent.RelatedContentTimestamp = _node.NodeTimestamp;
                    using (new SystemAccount())
                        stateContent.Save();
                }
                //Debug.WriteLine("##WF> RelatedContentProtector released: " + _node.Path);
            }


            private bool _disposed;
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            private void Dispose(bool disposing)
            {
                if (!this._disposed)
                    if (disposing)
                        this.Release();
                _disposed = true;
            }
            ~RelatedContentProtector()
            {
                Dispose(false);
            }

        }
    }
}
