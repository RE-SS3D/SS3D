using System;
using System.Collections.Generic;
using System.Text;
using FishNet.Broadcast;
using SS3D.Permissions;

namespace SS3D.Systems.Comms
{
    /// <summary>Non-diegetic chat channels (comms.md §7) shown in the lobby/Escape shell.</summary>
    public enum OocChannel : byte
    {
        /// <summary>Every connected player.</summary>
        Ooc = 0,
        /// <summary>Spawned players near the sender's body; not kept in history.</summary>
        Looc = 1,
        /// <summary>Administrators only.</summary>
        Admin = 2,
    }

    /// <summary>Server → client chat line. <see cref="Id"/> is a per-server-session sequence used to dedupe history.</summary>
    public struct OocChatMessage : IBroadcast
    {
        public long Id;
        public OocChannel Channel;
        /// <summary>Sender ckey; empty for system notices.</summary>
        public string Sender;
        public ServerRoleTypes SenderRole;
        public string Text;
        public long UnixSeconds;
        public bool IsSystem;
    }

    /// <summary>Client → server chat request; the server resolves the sender from the connection.</summary>
    public struct OocChatSendMessage : IBroadcast
    {
        public OocChannel Channel;
        public string Text;
    }

    /// <summary>Client → server: replay the OOC (and, for admins, Admin) history to the sender.</summary>
    public struct OocChatHistoryRequest : IBroadcast
    {
        /// <summary>Highest id the client already holds; only newer lines are replayed.</summary>
        public long AfterId;
    }

    /// <summary>Pure validation rules for OOC chat (EditMode-tested).</summary>
    public static class OocChatRules
    {
        public const int MaxLength = 256;
        public const float LoocRange = 10f;

        /// <summary>Strips control characters, collapses whitespace runs and clamps length.</summary>
        public static bool TryNormalize(string text, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            StringBuilder builder = new(Math.Min(text.Length, MaxLength));
            bool pendingSpace = false;
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace) builder.Append(' ');
                pendingSpace = false;
                builder.Append(c);
                if (builder.Length >= MaxLength) break;
            }

            if (builder.Length == 0) return false;
            normalized = builder.ToString();
            return true;
        }

        public static bool IsValidChannel(OocChannel channel) => Enum.IsDefined(typeof(OocChannel), channel);

        public static bool KeepsHistory(OocChannel channel) => channel != OocChannel.Looc;
    }

    /// <summary>Per-sender sliding window: at most <see cref="MaxMessages"/> within <see cref="WindowSeconds"/>.</summary>
    public sealed class OocRateLimiter
    {
        public const int MaxMessages = 5;
        public const float WindowSeconds = 5f;

        private readonly Dictionary<string, Queue<float>> _sent = new();

        public bool TryConsume(string sender, float now)
        {
            if (!_sent.TryGetValue(sender, out Queue<float> times))
            {
                times = new Queue<float>();
                _sent[sender] = times;
            }

            while (times.Count > 0 && now - times.Peek() >= WindowSeconds) times.Dequeue();
            if (times.Count >= MaxMessages) return false;
            times.Enqueue(now);
            return true;
        }

        public void Forget(string sender) => _sent.Remove(sender);
    }

    /// <summary>Bounded chat log; oldest lines drop first.</summary>
    public sealed class OocChatHistory
    {
        private readonly int _capacity;
        private readonly LinkedList<OocChatMessage> _messages = new();

        public OocChatHistory(int capacity) => _capacity = Math.Max(1, capacity);

        public IEnumerable<OocChatMessage> Messages => _messages;

        public int Count => _messages.Count;

        public long LastId => _messages.Count > 0 ? _messages.Last.Value.Id : 0;

        /// <summary>
        /// Inserts in id order. A history replay can arrive after live lines, so older ids are
        /// placed before them; returns false for a duplicate or an id older than a full log.
        /// </summary>
        public bool Add(OocChatMessage message)
        {
            LinkedListNode<OocChatMessage> node = _messages.Last;
            while (node != null && node.Value.Id > message.Id) node = node.Previous;
            if (node != null && node.Value.Id == message.Id) return false;
            if (node == null && _messages.Count >= _capacity) return false;

            if (node == null) _messages.AddFirst(message);
            else _messages.AddAfter(node, message);
            while (_messages.Count > _capacity) _messages.RemoveFirst();
            return true;
        }

        public void Clear() => _messages.Clear();
    }
}
