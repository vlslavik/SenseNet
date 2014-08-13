﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Communication.Messaging;
using System.Diagnostics;
using SenseNet.Diagnostics;


namespace SenseNet.Communication.Messaging
{
    public class ClusterChannelMonitor : ISnService
    {
        private static Timer _pingTimer;
        private static Timer _pongTimer;
        private static List<string> _lastResponses = new List<string>();
        private static List<string> _currentResponses;
        private static bool _receiverEnabled;
        private static Guid _actualPingId;

        public bool Start()
        {
            ContentRepository.DistributedApplication.ClusterChannel.MessageReceived += new MessageReceivedEventHandler(ClusterChannel_MessageReceived);
            _pingTimer = new Timer() { Enabled = true, Interval = RepositoryConfiguration.ClusterChannelMonitorInterval * 1000 };
            _pingTimer.Elapsed += new ElapsedEventHandler(PingTimer_Elapsed);
            _pongTimer = new Timer() { Enabled = false, Interval = RepositoryConfiguration.ClusterChannelMonitorTimeout * 1000 };
            _pongTimer.Elapsed += new ElapsedEventHandler(PongTimer_Elapsed);

            InitializeRecoverer();

            return true;
        }

        /// <summary>
        /// Shuts down the service. Called when the Repository is finishing.
        /// </summary>
        public void Shutdown()
        {
            ContentRepository.DistributedApplication.ClusterChannel.MessageReceived -= new MessageReceivedEventHandler(ClusterChannel_MessageReceived);

            _pingTimer.Elapsed -= new ElapsedEventHandler(PingTimer_Elapsed);
            _pingTimer.Stop();
            _pingTimer.Dispose();

            _pongTimer.Elapsed -= new ElapsedEventHandler(PongTimer_Elapsed);
            _pongTimer.Stop();
            _pongTimer.Dispose();

            Debug.WriteLine(String.Format("ClusterChannelMonitor> Shutdown ({0}): ", ContentRepository.DistributedApplication.ClusterChannel.ReceiverName));
        }

        void PingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!ContentRepository.DistributedApplication.ClusterChannel.AllowMessageProcessing)
                return;

            RecoverChannels();

            var receiverName=ContentRepository.DistributedApplication.ClusterChannel.ReceiverName;

            _currentResponses = new List<string>();
            _pongTimer.Enabled = true;
            _receiverEnabled = true; // receiving is disabled before sending the first ping message
            _pongTimer.Start();

            //___dump();

            var notResponsiveChannels = _channels.Where(c => c.NeedToRecover && !c.Running && c.AlreadyStarted).Select(c => c.Name).ToArray();
            if (notResponsiveChannels.Length > 0)
                Debug.WriteLine(String.Format("ClusterChannelMonitor> notResponsiveChannels ({0}): {1}", receiverName, String.Join(", ", notResponsiveChannels)));

            var pingMessage = new PingMessage(notResponsiveChannels);
            _actualPingId = pingMessage.Id;
            pingMessage.Send();
            //Debug.WriteLine(String.Format("ClusterChannelMonitor> PING sent ({0}): Id = {1}", receiverName, _actualPingId));
        }

        void PongTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_receiverEnabled)
                return;

            _receiverEnabled = false;
            _pongTimer.Stop();
            _pongTimer.Enabled = false;

            var stoppedChannels = _lastResponses.Except(_currentResponses).ToArray();
            var startedChannels = _currentResponses.Except(_lastResponses).ToArray();

            if(stoppedChannels.Length > 0)
                ChannelStopped(stoppedChannels);

            if (startedChannels.Length > 0)
                ChannelStarted(startedChannels);

            _lastResponses = _currentResponses;
        }
        private void ChannelStarted(string[] startedChannels)
        {
            var receiverName = ContentRepository.DistributedApplication.ClusterChannel.ReceiverName;

            var allStartedChannels = _channels.Where(c => startedChannels.Contains(c.Name)).ToArray();
            var brandNewChannels = allStartedChannels.Where(c => !c.Repairing).ToArray();
            var repairedChannels = allStartedChannels.Where(c => c.Repairing).ToArray();

            if (brandNewChannels.Length > 0)
            {
                var startedNames = brandNewChannels.Select(c => c.Name).ToArray();
                Debug.WriteLine(String.Format("ClusterChannelMonitor> CHANNEL STARTED ({0}): {1}, Running channels: {2}", receiverName, String.Join(", ", startedNames), String.Join(", ", _currentResponses)));
                Logger.WriteInformation(
                    EventId.ClusterChannelMonitor.ChannelStarted,
                    String.Format("{0} cluster channel started: {1}", startedNames.Length, String.Join(", ", startedNames)),
                    categories: new[] { Logger.Category.Messaging },
                    properties: new Dictionary<string, object> { { "Name: ", receiverName }, { "Running channels", String.Join(", ", _currentResponses) } }
                );
            }
            if (repairedChannels.Length > 0)
            {
                var repairedNames = repairedChannels.Select(c => c.Name).ToArray();
                Debug.WriteLine(String.Format("ClusterChannelMonitor> CHANNEL REPAIRED ({0}): {1}, Running channels: {2}", receiverName, String.Join(", ", repairedNames), String.Join(", ", _currentResponses)));
                Logger.WriteInformation(
                    EventId.ClusterChannelMonitor.ChannelStarted,
                    String.Format("{0} cluster channel repaired: {1}", repairedNames.Length, String.Join(", ", repairedNames)),
                    categories: new[] { Logger.Category.Messaging },
                    properties: new Dictionary<string, object> { { "Name: ", receiverName }, { "Running channels", String.Join(", ", _currentResponses) } }
                );
            }

            foreach (var channel in allStartedChannels)
            {
                channel.AlreadyStarted = true;
                channel.Repairing = false;
                channel.Running = true;
            }

        }
        private void ChannelStopped(string[] stoppedChannels)
        {
            var receiverName = ContentRepository.DistributedApplication.ClusterChannel.ReceiverName;

            var allStoppedChannels = _channels.Where(c => stoppedChannels.Contains(c.Name)).ToArray();
            var brokenChannels = allStoppedChannels.Where(c => c.NeedToRecover).ToArray();
            var closedChannels = allStoppedChannels.Where(c => !c.NeedToRecover).ToArray();

            if (brokenChannels.Length > 0)
            {
                var brokenNames = brokenChannels.Select(c => c.Name).ToArray();
                Debug.WriteLine(String.Format("ClusterChannelMonitor> BROKEN CHANNELS ({0}): {1}, Running channels: {2}", receiverName, String.Join(", ", brokenNames), String.Join(", ", _currentResponses)));
                Logger.WriteCritical(
                        EventId.ClusterChannelMonitor.ChannelStopped,
                        String.Format("{0} cluster channel stopped: {1}", brokenChannels.Length, String.Join(", ", brokenNames)),
                        categories: new[] { Logger.Category.Messaging },
                        properties: new Dictionary<string, object> { { "Name: ", receiverName }, { "Running channels", String.Join(", ", _currentResponses) } }
                    );
            }
            if (closedChannels.Length > 0)
            {
                var closedNames = closedChannels.Select(c => c.Name).ToArray();
                Debug.WriteLine(String.Format("ClusterChannelMonitor> CLOSED CHANNELS ({0}): {1}, Running channels: {2}", receiverName, String.Join(", ", closedNames), String.Join(", ", _currentResponses)));
                Logger.WriteInformation(
                    EventId.ClusterChannelMonitor.ChannelStopped,
                    String.Format("{0} MSMQ channel stopped: {1}", closedChannels.Length, String.Join(", ", closedNames)),
                    categories: new[] { Logger.Category.Messaging },
                    properties: new Dictionary<string, object> { { "Name: ", receiverName }, { "Running channels", String.Join(", ", _currentResponses) } }
                );
            }

            var channels = _channels.Where(x => stoppedChannels.Contains(x.Name));
            foreach (var channel in channels)
                channel.Running = false;
        }

        void ClusterChannel_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            var receiverName = ContentRepository.DistributedApplication.ClusterChannel.ReceiverName;

            var message = args.Message;

            var pingMessage = message as PingMessage;
            if (pingMessage != null)
                ProcessPingMessage(pingMessage);

            if (!_receiverEnabled)
                return;

            var pongMessage = message as PongMessage;
            if (pongMessage == null)
                return;

            if (pongMessage.PingId != _actualPingId)
                return;

            var senderName = pongMessage.SenderInfo.ClusterMemberID;
            //Debug.WriteLine(String.Format("ClusterChannelMonitor> PONG received ({0}): Sender = {1}, Id = {2}", receiverName, senderName, pongMessage.PingId));
            if (!_currentResponses.Contains(senderName))
                _currentResponses.Add(senderName);
        }
        private void ProcessPingMessage(PingMessage message)
        {
            var receiverName = ContentRepository.DistributedApplication.ClusterChannel.ReceiverName;

            var senderName = message.SenderInfo.ClusterMemberID;
            var channel = _channels.FirstOrDefault(c => c.Name == senderName);
            if (channel == null)
                return;

            //Debug.WriteLine(String.Format("ClusterChannelMonitor> PING received ({0}): Sender = {1}, Id = {2}", receiverName, senderName, message.Id));
            channel.NeedToRecover = message.SenderInfo.NeedToRecover;
            if (channel.Name == ContentRepository.DistributedApplication.ClusterChannel.ReceiverName)
                channel.Running = true;
            if (message.NotResponsiveChannels.Contains(ContentRepository.DistributedApplication.ClusterChannel.ReceiverName))
            {
                //Debug.WriteLine("ClusterChannelMonitor> Recovering receiver channel (" + ContentRepository.DistributedApplication.ClusterChannel.ReceiverName + ")");
                RestartAllChannelsAsync();
            }
        }

        /*======================================================================================== Channel recovery */

        private class WorkingChannel
        {
            public string Name;
            public bool AlreadyStarted;
            public bool NeedToRecover;
            public bool Running;
            public bool Repairing;
        }
        List<WorkingChannel> _channels;

        private void InitializeRecoverer()
        {
            var clusterChannel = ContentRepository.DistributedApplication.ClusterChannel;

            var queues = new List<string>(); 
            queues.Add(clusterChannel.ReceiverName);
            queues.AddRange(clusterChannel.SenderNames);
            _channels = queues.Select(x => new WorkingChannel { Name = GetChannelName(x), NeedToRecover = false }).ToList();
            Debug.WriteLine("ClusterChannelMonitor> Initialized (" + ContentRepository.DistributedApplication.ClusterChannel.ReceiverName + ")");
        }
        private string GetChannelName(string configName)
        {
            if (configName == null)
                return null;
            if (configName[0] == '.')
                return ClusterMemberInfo.Current.Machine + configName.Substring(1); //#1: .\private$\ryan
            return configName.Replace("FormatName:DIRECT=TCP:", ""); //#2: FormatName:DIRECT=TCP:192.168.0.132\private$\sn6test
        }
        private void RecoverChannels()
        {
            var channels = _channels.Where(c => c.NeedToRecover && !c.Running && c.AlreadyStarted).ToArray();
            if (channels.Length == 0)
                return;

            foreach (var channel in channels)
                channel.Repairing = true;

            var names = channels.Select(c => c.Name).ToArray();

            //Debug.WriteLine("ClusterChannelMonitor> Recovering sender channels (" + ContentRepository.DistributedApplication.ClusterChannel.ReceiverName + "): " + String.Join(", ", names));
            RestartAllChannelsAsync();
        }

        private void RestartAllChannelsAsync()
        {
            var cc = ContentRepository.DistributedApplication.ClusterChannel;
            if (cc.RestartingAllChannels)
            {
                //Debug.WriteLine("ClusterChannelMonitor> ######## Recovering in progress (" + cc.ReceiverName + ")");
                return;
            }
            //Debug.WriteLine("ClusterChannelMonitor> ######## Recovering STARTED (" + cc.ReceiverName + ")");
            cc.RestartAllChannels();
            //Debug.WriteLine("ClusterChannelMonitor> Recovering finished (" + cc.ReceiverName + ")");
        }

        private void ___dump()
        {
            var sb = new StringBuilder();
            sb.Append("ClusterChannelMonitor> ").Append(ContentRepository.DistributedApplication.ClusterChannel.ReceiverName).Append(" ===> ");
            foreach (var c in _channels.OrderBy(c => c.Name))
            {
                sb.Append(c.Name).Append(": ");
                if (c.NeedToRecover && c.Running)   sb.Append("   runs (");
                if (c.NeedToRecover && !c.Running)  sb.Append(" BROKEN (");
                if (!c.NeedToRecover && c.Running)  sb.Append("closing (");
                if (!c.NeedToRecover && !c.Running) sb.Append(" closed (");
                sb.Append("ast: ").Append(c.AlreadyStarted ? "y, " : "n, ");
                sb.Append("rec: ").Append(c.NeedToRecover ? "y, " : "n, ");
                sb.Append("rep: ").Append(c.Repairing ? "y, " : "n, ");
                sb.Append(") | ");
            }
            Debug.WriteLine(sb.ToString());
        }


    }
}