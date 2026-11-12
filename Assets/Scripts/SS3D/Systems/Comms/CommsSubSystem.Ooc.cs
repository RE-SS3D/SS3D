using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// OOC / LOOC / Admin chat (comms.md §7). Senders are resolved from the connection; the
    /// server keeps a bounded OOC and Admin history for clients that join later.
    /// </summary>
    public partial class CommsSubSystem
    {
        private const int OocHistoryCapacity = 200;

        private readonly OocChatHistory _serverOocHistory = new(OocHistoryCapacity);
        private readonly OocChatHistory _serverAdminHistory = new(OocHistoryCapacity);
        private readonly OocRateLimiter _oocRateLimiter = new();
        private long _nextOocId;

        private readonly OocChatHistory _clientOocLog = new(OocHistoryCapacity);

        /// <summary>Client: the received chat lines (all channels) in server order.</summary>
        public IEnumerable<OocChatMessage> OocLog => _clientOocLog.Messages;

        /// <summary>Client: increments whenever <see cref="OocLog"/> changes.</summary>
        public int OocLogVersion { get; private set; }

        public event Action<OocChatMessage> OocMessageReceived;

        private void RegisterOocBroadcasts()
        {
            InstanceFinder.ClientManager.RegisterBroadcast<OocChatMessage>(OnClientReceiveOoc);
            InstanceFinder.ServerManager.RegisterBroadcast<OocChatSendMessage>(OnServerReceiveOocSend);
            InstanceFinder.ServerManager.RegisterBroadcast<OocChatHistoryRequest>(OnServerReceiveOocHistoryRequest);
        }

        private void UnregisterOocBroadcasts()
        {
            if (InstanceFinder.ClientManager != null)
                InstanceFinder.ClientManager.UnregisterBroadcast<OocChatMessage>(OnClientReceiveOoc);
            if (InstanceFinder.ServerManager != null)
            {
                InstanceFinder.ServerManager.UnregisterBroadcast<OocChatSendMessage>(OnServerReceiveOocSend);
                InstanceFinder.ServerManager.UnregisterBroadcast<OocChatHistoryRequest>(OnServerReceiveOocHistoryRequest);
            }

            _clientOocLog.Clear();
            OocLogVersion++;
        }

        /// <summary>Client: sends a chat line; the server validates channel access and rate.</summary>
        public void SendOoc(OocChannel channel, string text)
        {
            if (!OocChatRules.TryNormalize(text, out string normalized)) return;
            // A bare /bug opens the bug report dialog; /bug <note> still files from the server.
            if (BugReports.BugCommand.TryParse(normalized, out string bugNote) && bugNote.Length == 0
                && BugReports.BugReportRequests.RequestOpen()) return;
            if (InstanceFinder.ClientManager == null || !InstanceFinder.ClientManager.Started) return;
            InstanceFinder.ClientManager.Broadcast(new OocChatSendMessage { Channel = channel, Text = normalized });
        }

        /// <summary>Client: asks for lines newer than the local log (once per connection, after the player record exists).</summary>
        public void RequestOocHistory()
        {
            if (InstanceFinder.ClientManager == null || !InstanceFinder.ClientManager.Started) return;
            InstanceFinder.ClientManager.Broadcast(new OocChatHistoryRequest { AfterId = _clientOocLog.LastId });
        }

        /// <summary>Server: a system line (round state, notices) on a history-keeping channel.</summary>
        public void PostOocSystem(OocChannel channel, string text)
        {
            if (!IsServer || !OocChatRules.KeepsHistory(channel) || !OocChatRules.TryNormalize(text, out text)) return;
            DispatchOoc(NewOocMessage(channel, "", ServerRoleTypes.None, text, true));
        }

        private void OnClientReceiveOoc(OocChatMessage message)
        {
            if (!_clientOocLog.Add(message)) return;
            OocLogVersion++;
            OocMessageReceived?.Invoke(message);
        }

        private void OnServerReceiveOocSend(NetworkConnection conn, OocChatSendMessage request)
        {
            if (!OocChatRules.IsValidChannel(request.Channel)) return;
            if (!OocChatRules.TryNormalize(request.Text, out string text)) return;
            if (!SubSystems.TryGet(out PlayerSubSystem players)) return;
            string ckey = players.GetCkey(conn);
            if (string.IsNullOrEmpty(ckey)) return;

            ServerRoleTypes role = GetRole(ckey);
            if (request.Channel == OocChannel.Admin && role < ServerRoleTypes.Administrator) return;

            if (!_oocRateLimiter.TryConsume(ckey, Time.unscaledTime))
            {
                SendOocNotice(conn, request.Channel, "You are sending messages too quickly.");
                return;
            }

            // Chat command: /bug <note> goes to the server's bug report file, not to chat.
            if (BugReports.BugCommand.TryParse(text, out string bugNote))
            {
                SendOocNotice(conn, request.Channel, BugReports.BugReportRequests.FileChatNote(conn, ckey, bugNote));
                return;
            }

            if (request.Channel == OocChannel.Looc)
            {
                SendLooc(conn, ckey, role, text);
                return;
            }

            DispatchOoc(NewOocMessage(request.Channel, ckey, role, text, false));
        }

        private void OnServerReceiveOocHistoryRequest(NetworkConnection conn, OocChatHistoryRequest request)
        {
            if (!SubSystems.TryGet(out PlayerSubSystem players)) return;
            string ckey = players.GetCkey(conn);
            if (string.IsNullOrEmpty(ckey)) return;

            List<OocChatMessage> replay = new();
            foreach (OocChatMessage message in _serverOocHistory.Messages)
                if (message.Id > request.AfterId) replay.Add(message);
            if (GetRole(ckey) >= ServerRoleTypes.Administrator)
                foreach (OocChatMessage message in _serverAdminHistory.Messages)
                    if (message.Id > request.AfterId) replay.Add(message);

            replay.Sort((a, b) => a.Id.CompareTo(b.Id));
            foreach (OocChatMessage message in replay)
                InstanceFinder.ServerManager.Broadcast(conn, message);
        }

        private void SendLooc(NetworkConnection conn, string ckey, ServerRoleTypes role, string text)
        {
            if (!SubSystems.TryGet(out EntitySubSystem entities) || !entities.TryGetSpawnedEntity(conn, out Entity sender))
            {
                SendOocNotice(conn, OocChannel.Looc, "LOOC reaches players near your character; join the round first.");
                return;
            }

            HashSet<NetworkConnection> recipients = new() { conn };
            Vector3 origin = sender.transform.position;
            foreach (NetworkConnection client in InstanceFinder.ServerManager.Clients.Values)
            {
                if (entities.TryGetSpawnedEntity(client, out Entity listener)
                    && Vector3.Distance(listener.transform.position, origin) <= OocChatRules.LoocRange)
                    recipients.Add(client);
            }

            OocChatMessage message = NewOocMessage(OocChannel.Looc, ckey, role, text, false);
            AppendOocLog(message);
            InstanceFinder.ServerManager.Broadcast(recipients, message);
        }

        private void DispatchOoc(OocChatMessage message)
        {
            AppendOocLog(message);
            if (message.Channel == OocChannel.Admin)
            {
                _serverAdminHistory.Add(message);
                HashSet<NetworkConnection> admins = new();
                if (SubSystems.TryGet(out PlayerSubSystem players))
                    foreach (NetworkConnection client in InstanceFinder.ServerManager.Clients.Values)
                        if (GetRole(players.GetCkey(client)) >= ServerRoleTypes.Administrator) admins.Add(client);
                InstanceFinder.ServerManager.Broadcast(admins, message);
                return;
            }

            _serverOocHistory.Add(message);
            InstanceFinder.ServerManager.Broadcast(message);
        }

        private void SendOocNotice(NetworkConnection conn, OocChannel channel, string text)
        {
            InstanceFinder.ServerManager.Broadcast(conn, NewOocMessage(channel, "", ServerRoleTypes.None, text, true));
        }

        private OocChatMessage NewOocMessage(OocChannel channel, string sender, ServerRoleTypes role, string text, bool system) => new()
        {
            Id = ++_nextOocId,
            Channel = channel,
            Sender = sender,
            SenderRole = role,
            Text = text,
            UnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsSystem = system,
        };

        private static ServerRoleTypes GetRole(string ckey)
        {
            return !string.IsNullOrEmpty(ckey) && SubSystems.TryGet(out PermissionSubSystem permissions)
                && permissions.TryGetUserRole(ckey, out ServerRoleTypes role) ? role : ServerRoleTypes.None;
        }

        private void AppendOocLog(OocChatMessage message)
        {
            AppendServerLogLine($"[{message.Channel.ToString().ToUpperInvariant()}] [{(message.IsSystem ? "System" : message.Sender)}] {message.Text}");
        }
    }
}
