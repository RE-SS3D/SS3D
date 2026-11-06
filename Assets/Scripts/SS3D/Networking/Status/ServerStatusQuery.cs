using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using LiteNetLib;

namespace SS3D.Networking.Status
{
    /// <summary>Outcome of one <see cref="ServerStatusQuery"/> request.</summary>
    public readonly struct ServerStatusResult
    {
        public readonly bool Reachable;
        public readonly ServerStatus Status;
        public readonly int PingMilliseconds;

        public ServerStatusResult(bool reachable, ServerStatus status, int pingMilliseconds)
        {
            Reachable = reachable;
            Status = status;
            PingMilliseconds = pingMilliseconds;
        }

        public static ServerStatusResult Unreachable => new(false, default, 0);
    }

    /// <summary>
    /// Client side of the status query: its own small LiteNetLib socket, separate from FishNet's
    /// client. Each query sends <see cref="ProbeCount"/> nonce-tagged probes
    /// (<see cref="ServerStatusPacket.Request(uint)"/>) a few tens of milliseconds apart and reports
    /// the median round trip, so one slow packet does not move the number.
    /// <para>
    /// Replies are timestamped on the socket's receive thread the moment they arrive
    /// (<see cref="NetManager.UnsyncedEvents"/>), not when the game loop next looks, so the ping does
    /// not include frame time; the nonce ties each reply to the exact probe it answers. Call
    /// <see cref="Tick"/> every frame on the main thread: it sends due probes and delivers results
    /// through <see cref="Completed"/>. A server that predates the probe is retried once with the
    /// original request and reports a single sample. A server that does not answer within
    /// <see cref="TimeoutSeconds"/> (offline, firewalled, or older than the query) is unreachable.
    /// </para>
    /// </summary>
    public sealed class ServerStatusQuery : IDisposable
    {
        public const float TimeoutSeconds = 3f;
        public const int ProbeCount = 4;
        private const float ProbeSpacingSeconds = 0.075f;
        /// <summary>After the last probe is out, how long to wait for slow replies before settling for the ones in.</summary>
        private const float StragglerSeconds = 0.4f;
        /// <summary>With no probe answered this long after the first, also try the original request.</summary>
        private const float LegacyFallbackSeconds = 0.75f;

        private readonly EventBasedNetListener _listener = new();
        private readonly NetManager _socket;
        private readonly List<Pending> _pending = new();
        private readonly ConcurrentQueue<Incoming> _incoming = new();
        private uint _nextNonce = (uint)new Random().Next();

        public event Action<string, ServerStatusResult> Completed;

        public ServerStatusQuery()
        {
            _listener.NetworkReceiveUnconnectedEvent += HandleUnconnected;
            _socket = new NetManager(_listener) { UnconnectedMessagesEnabled = true, UnsyncedEvents = true };
            _socket.Start();
        }

        public bool IsPending(string key) => _pending.Exists(p => p.Key == key);

        /// <summary>Starts a query; a still-running query with the same key is replaced.</summary>
        public void Query(string key, string host, ushort port)
        {
            _pending.RemoveAll(p => p.Key == key);
            Pending pending = new(key, port, Now());
            if (IPAddress.TryParse(host, out IPAddress literal))
            {
                pending.Address = Normalize(literal);
            }
            else
            {
                pending.Resolve = Task.Run(() => Dns.GetHostAddresses(host));
            }

            _pending.Add(pending);
        }

        public void Tick()
        {
            if (!_socket.IsRunning)
            {
                FailAll();
                return;
            }

            while (_incoming.TryDequeue(out Incoming reply))
            {
                Receive(reply);
            }

            double now = Now();
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                Pending pending = _pending[i];
                if (pending.Address == null && pending.Resolve is { IsCompleted: true })
                {
                    pending.Address = pending.Resolve.Status == TaskStatus.RanToCompletion
                        ? Pick(pending.Resolve.Result)
                        : null;
                    pending.Resolve = null;
                    if (pending.Address == null)
                    {
                        Finish(i, ServerStatusResult.Unreachable);
                        continue;
                    }
                }

                if (pending.Answered == ProbeCount
                    || (pending.Answered > 0 && pending.ProbesSent == ProbeCount && now - pending.LastProbeSent > StragglerSeconds))
                {
                    Finish(i, new ServerStatusResult(true, pending.Status, MedianMilliseconds(pending)));
                    continue;
                }

                if (now - pending.Started > TimeoutSeconds)
                {
                    Finish(i, pending.Answered > 0
                        ? new ServerStatusResult(true, pending.Status, MedianMilliseconds(pending))
                        : ServerStatusResult.Unreachable);
                    continue;
                }

                if (pending.Address != null)
                {
                    SendDue(pending, now);
                }
            }
        }

        public void Dispose()
        {
            _pending.Clear();
            _listener.NetworkReceiveUnconnectedEvent -= HandleUnconnected;
            _socket.Stop();
            while (_incoming.TryDequeue(out _))
            {
            }
        }

        private void SendDue(Pending pending, double now)
        {
            IPEndPoint endpoint = new(pending.Address, pending.Port);
            if (pending.ProbesSent < ProbeCount && (pending.ProbesSent == 0 || now - pending.LastProbeSent >= ProbeSpacingSeconds))
            {
                int index = pending.ProbesSent;
                uint nonce = _nextNonce++;
                pending.Nonces[index] = nonce;
                pending.LastProbeSent = now;
                if (index == 0)
                {
                    pending.FirstProbeSent = now;
                }

                pending.ProbesSent++;
                // Stamped as late as possible so the send call itself is not counted.
                pending.SentTicks[index] = Stopwatch.GetTimestamp();
                _socket.SendUnconnectedMessage(ServerStatusPacket.Request(nonce), endpoint);
            }

            if (!pending.LegacySent && pending.Answered == 0 && pending.ProbesSent > 0
                && now - pending.FirstProbeSent > LegacyFallbackSeconds)
            {
                pending.LegacySent = true;
                pending.LegacySentTicks = Stopwatch.GetTimestamp();
                _socket.SendUnconnectedMessage(ServerStatusPacket.Request(), endpoint);
            }
        }

        /// <summary>Receive thread: timestamp first, decode, hand to the main thread. Touches no other state.</summary>
        private void HandleUnconnected(IPEndPoint remote, NetPacketReader reader, UnconnectedMessageType type)
        {
            long arrived = Stopwatch.GetTimestamp();
            byte[] data = reader.GetRemainingBytes();
            reader.Recycle();
            if (type != UnconnectedMessageType.BasicMessage
                || !ServerStatusPacket.TryDecodeReply(data, out ServerStatus status, out bool hasNonce, out uint nonce))
            {
                return;
            }

            _incoming.Enqueue(new Incoming(Normalize(remote.Address), remote.Port, status, hasNonce, nonce, arrived));
        }

        private void Receive(Incoming reply)
        {
            int index = _pending.FindIndex(p => p.Port == reply.Port && reply.Address.Equals(p.Address));
            if (index < 0)
            {
                return;
            }

            Pending pending = _pending[index];
            if (reply.HasNonce)
            {
                for (int i = 0; i < pending.ProbesSent; i++)
                {
                    if (pending.Nonces[i] == reply.Nonce && pending.RoundTripTicks[i] < 0)
                    {
                        pending.RoundTripTicks[i] = Math.Max(0, reply.Ticks - pending.SentTicks[i]);
                        pending.Answered++;
                        pending.Status = reply.Status;
                        return;
                    }
                }

                return;
            }

            // Reply to the original request: no nonce, so a single sample timed from that request.
            if (pending.Answered > 0 || (!pending.LegacySent && pending.ProbesSent == 0))
            {
                return;
            }

            long sent = pending.LegacySent ? pending.LegacySentTicks : pending.SentTicks[0];
            Finish(index, new ServerStatusResult(true, reply.Status, ToMilliseconds(Math.Max(0, reply.Ticks - sent))));
        }

        private void Finish(int index, ServerStatusResult result)
        {
            string key = _pending[index].Key;
            _pending.RemoveAt(index);
            Completed?.Invoke(key, result);
        }

        private void FailAll()
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                Finish(i, ServerStatusResult.Unreachable);
            }
        }

        /// <summary>Median of the answered probes (mean of the middle two when even), at least 1 ms so a LAN server never reads "0 ms".</summary>
        private static int MedianMilliseconds(Pending pending)
        {
            List<long> samples = new(ProbeCount);
            foreach (long ticks in pending.RoundTripTicks)
            {
                if (ticks >= 0)
                {
                    samples.Add(ticks);
                }
            }

            samples.Sort();
            int mid = samples.Count / 2;
            long median = samples.Count % 2 == 1 ? samples[mid] : (samples[mid - 1] + samples[mid]) / 2;
            return ToMilliseconds(median);
        }

        private static int ToMilliseconds(long ticks) =>
            Math.Max(1, (int)Math.Round(ticks * 1000.0 / Stopwatch.Frequency));

        private static double Now() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

        private static IPAddress Pick(IPAddress[] addresses)
        {
            IPAddress fallback = null;
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address;
                }

                fallback ??= address;
            }

            return fallback == null ? null : Normalize(fallback);
        }

        private static IPAddress Normalize(IPAddress address) =>
            address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        private readonly struct Incoming
        {
            public readonly IPAddress Address;
            public readonly int Port;
            public readonly ServerStatus Status;
            public readonly bool HasNonce;
            public readonly uint Nonce;
            public readonly long Ticks;

            public Incoming(IPAddress address, int port, ServerStatus status, bool hasNonce, uint nonce, long ticks)
            {
                Address = address;
                Port = port;
                Status = status;
                HasNonce = hasNonce;
                Nonce = nonce;
                Ticks = ticks;
            }
        }

        private sealed class Pending
        {
            public readonly string Key;
            public readonly ushort Port;
            public readonly double Started;
            public readonly uint[] Nonces = new uint[ProbeCount];
            public readonly long[] SentTicks = new long[ProbeCount];
            public readonly long[] RoundTripTicks = { -1, -1, -1, -1 };
            public IPAddress Address;
            public Task<IPAddress[]> Resolve;
            public int ProbesSent;
            public int Answered;
            public double FirstProbeSent;
            public double LastProbeSent;
            public ServerStatus Status;
            public bool LegacySent;
            public long LegacySentTicks;

            public Pending(string key, ushort port, double started)
            {
                Key = key;
                Port = port;
                Started = started;
            }
        }
    }
}
