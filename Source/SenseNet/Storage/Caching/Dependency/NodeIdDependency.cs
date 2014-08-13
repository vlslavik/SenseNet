﻿using System;
using System.Web.Caching;
using SenseNet.Communication.Messaging;
using System.Threading;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public class NodeIdDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private int _nodeId;

            private FireChangedDistributedAction(int nodeId)
            {
                _nodeId = nodeId;
            }

            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                FireChangedPrivate(_nodeId);
            }

            internal static void Trigger(int nodeId)
            {
                new FireChangedDistributedAction(nodeId).Execute();
            }
        }
        #endregion

        private int _nodeId;
        //private static event EventHandler<EventArgs<int>> Changed;
        private static EventServer<int> Changed = new EventServer<int>(RepositoryConfiguration.NodeIdDependencyEventPartitions);

        public NodeIdDependency(int nodeId)
        {
            _nodeId = nodeId;
            try
            {
                lock (PortletDependency._eventSync)
                {
                    //Changed += NodeIdDependency_NodeIdChanged;
                    Changed.TheEvent += NodeIdDependency_NodeIdChanged;
                }
            }
            finally
            {
                this.FinishInit();
            }
        }

        void NodeIdDependency_NodeIdChanged(object sender, EventArgs<int> e)
        {
            if (_nodeId == e.Data)
                NotifyDependencyChanged(this, e);
        }

        protected override void DependencyDispose()
        {
            lock (PortletDependency._eventSync)
            {
                //if (Changed != null)
                //    Changed -= NodeIdDependency_NodeIdChanged;
                Changed.TheEvent -= NodeIdDependency_NodeIdChanged;
            }
        }

        public static void FireChanged(int nodeId)
        {
            FireChangedDistributedAction.Trigger(nodeId);
        }
        private static void FireChangedPrivate(int nodeId)
        {
            lock (PortletDependency._eventSync)
            {
                //if (Changed != null)
                //    Changed(null, new EventArgs<int>(nodeId));
                Changed.Fire(null, nodeId);
            }
        }
    }
}
