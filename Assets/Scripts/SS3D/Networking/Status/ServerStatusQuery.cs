using System;
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
    /// client, sends <see cref="ServerStatusPacket.Request"/> as an unconnected message and matches
    /// replies by endpoint. Call <see cref="Tick"/> every frame on the main thread; results arrive
    /// through <see cref="Completed"/> from there. A server that does not answer within
    /// <see cref="TimeoutSeconds"/> (offline, firewalled, or older than the query) is unreachable.
    /// </summary>
    public sealed class ServerStatusQuery : IDisposable
    {
        public const float TimeoutSeconds = 3f;
        private const float ResendSeconds = 1f;

        private readonly EventBasedNetListener _listener = new();
        private readonly NetManager _socket;
        private readonly List<Pending> _pending = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        public event Action<string, ServerStatusResult> Completed;

        public ServerStatusQuery()
        {
            _listener.NetworkReceiveUnconnectedEvent += HandleUnconnected;
            _socket = new NetManager(_listener) { UnconnectedMessagesEnabled = true };
            _socket.Start();
        }

        public bool IsPending(string key) => _pending.Exists(p => p.Key == key);

        /// <summary>Starts a query; a still-running query with the same key is replaced.</summary>
        public void Query(string key, string host, ushort port)
        {
            _pending.RemoveAll(p => p.Key == key);
            Pending pending = new(key, port, _clock.Elapsed.TotalSeconds);
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

            _socket.PollEvents();
            double now = _clock.Elapsed.TotalSeconds;
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

                if (now - pending.Started > TimeoutSeconds)
                {
                    Finish(i, ServerStatusResult.Unreachable);
                    continue;
                }

                if (pending.Address != null && (pending.LastSent < 0 || now - pending.LastSent > ResendSeconds))
                {
                    if (pending.LastSent < 0)
                    {
                        pending.Sent = now;
                    }

                    pending.LastSent = now;
                    _socket.SendUnconnectedMessage(ServerStatusPacket.Request(), new IPEndPoint(pending.Address, pending.Port));
                }
            }
        }

        public void Dispose()
        {
            _pending.Clear();
            _listener.NetworkReceiveUnconnectedEvent -= HandleUnconnected;
            _socket.Stop();
        }

        private void HandleUnconnected(IPEndPoint remote, NetPacketReader reader, UnconnectedMessageType type)
        {
            byte[] data = reader.GetRemainingBytes();
            reader.Recycle();
            if (type != UnconnectedMessageType.BasicMessage || !ServerStatusPacket.TryDecodeReply(data, out ServerStatus status))
            {
                return;
            }

            IPAddress address = Normalize(remote.Address);
            int index = _pending.FindIndex(p => p.Port == remote.Port && address.Equals(p.Address));
            if (index < 0)
            {
                return;
            }

            int ping = (int)Math.Round((_clock.Elapsed.TotalSeconds - _pending[index].Sent) * 1000);
            Finish(index, new ServerStatusResult(true, status, Math.Max(0, ping)));
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

        private sealed class Pending
        {
            public readonly string Key;
            public readonly ushort Port;
            public readonly double Started;
            public IPAddress Address;
            public Task<IPAddress[]> Resolve;
            public double Sent;
            public double LastSent = -1;

            public Pending(string key, ushort port, double started)
            {
                Key = key;
                Port = port;
                Started = started;
            }
        }
    }
}
