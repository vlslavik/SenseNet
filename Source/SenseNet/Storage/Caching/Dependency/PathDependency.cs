﻿using System;
using System.Collections.Generic;
using System.Web.Caching;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public class PathDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private string _path;

            private FireChangedDistributedAction(string path)
            {
                _path = path;
            }

            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                FireChangedPrivate(_path);
            }

            internal static void Trigger(string path)
            {
                new FireChangedDistributedAction(path).Execute();
            }
        }
        // -----------------------------------------------------------------------------------------
        #endregion

        private string _path;
        //private static event EventHandler<EventArgs<string>> SubtreeChanged;
        private static EventServer<string> Changed = new EventServer<string>(RepositoryConfiguration.PathDependencyEventPartitions);

        public PathDependency(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            _path = path.ToLowerInvariant();
            try
            {
                lock (PortletDependency._eventSync)
                {
                    //SubtreeChanged += PathDependency_SubtreeChanged;
                    Changed.TheEvent += PathDependency_SubtreeChanged;
                }
            }
            finally
            {
                this.FinishInit();
            }
        }

        void PathDependency_SubtreeChanged(object sender, EventArgs<string> e)
        {

            string path = e.Data.ToLowerInvariant();

            // Path matches?
            bool match = _path == path;

            // If does not match, path starts with?
            if (!match)
                match = _path.StartsWith(string.Concat(e.Data, RepositoryPath.PathSeparator), StringComparison.OrdinalIgnoreCase);

            if (match)
            {
                NotifyDependencyChanged(this, e);

                SenseNet.Diagnostics.Logger.WriteVerbose("Cache invalidated by path.", new Dictionary<string, object> { { "Path", _path } });
            }
        }

        protected override void DependencyDispose()
        {
            lock (PortletDependency._eventSync)
            {
                //if (SubtreeChanged != null)
                //    SubtreeChanged -= PathDependency_SubtreeChanged;
                Changed.TheEvent -= PathDependency_SubtreeChanged;
            }
        }

        public static void FireChanged(string path)
        {
            FireChangedDistributedAction.Trigger(path);
        }
        private static void FireChangedPrivate(string path)
        {
            lock (PortletDependency._eventSync)
            {
                //if (SubtreeChanged != null)
                //    SubtreeChanged(null, new EventArgs<string>(path));
                Changed.Fire(null, path);
            }
        }
    }
}
