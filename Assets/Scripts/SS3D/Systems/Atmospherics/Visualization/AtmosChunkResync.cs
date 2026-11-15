using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Server: the clients that asked for every atmos chunk once, and which chunks each still has to get. The dirty-chunk
    /// broadcast (<see cref="AtmosDirtyChunkTracker"/>) only sends what changed since it last reported, from awake cells, so
    /// a pure client that joined after the station settled never hears of its calm rooms. The map editor's Room air
    /// (Documents/architecture/2026-11_map-editor-systems-views.md Q2, Phase 5) tints every floor by pressure, so its
    /// client is sent the whole station through the same patch path, a few chunks per call; after that the broadcast
    /// keeps it current like any client. Pure C#.
    /// </summary>
    public sealed class AtmosChunkResync
    {
        private readonly Dictionary<int, Queue<Vector2Int>> _pending = new();
        // Clients whose whole station has gone out: another "on" is ignored until an "off" (End).
        private readonly HashSet<int> _done = new();
        // Clients to send the whole station again once a new simulation is up (a station template restore).
        private readonly HashSet<int> _restart = new();
        private readonly List<int> _clientIds = new();
        private int _next;

        public bool HasWork => _pending.Count > 0;

        /// <summary>Whether <paramref name="clientId"/> still has chunks to get.</summary>
        public bool IsPending(int clientId) => _pending.ContainsKey(clientId);

        /// <summary>Whether <paramref name="clientId"/>'s resync is running or done, so a repeated "on" changes nothing.</summary>
        public bool IsStarted(int clientId) => _pending.ContainsKey(clientId) || _done.Contains(clientId);

        /// <summary>
        /// Queues every one of <paramref name="chunks"/> for <paramref name="clientId"/>. Ignored while its resync runs or
        /// after it finished: only an <see cref="End"/> (Room air off) lets the next "on" send the station again.
        /// </summary>
        public void Begin(int clientId, IEnumerable<Vector2Int> chunks)
        {
            if (IsStarted(clientId))
                return;

            Queue<Vector2Int> queue = new(chunks ?? Array.Empty<Vector2Int>());
            if (queue.Count == 0)
                _done.Add(clientId);
            else
                _pending[clientId] = queue;
        }

        /// <summary>Room air went off, the editor closed or the client left: nothing more is sent to it.</summary>
        public void End(int clientId)
        {
            _pending.Remove(clientId);
            _done.Remove(clientId);
            _restart.Remove(clientId);
        }

        /// <summary>
        /// The simulation is being replaced (a station template restore): every client with Room air on is sent the new
        /// station once it is up (<see cref="TakeRestarts"/>).
        /// </summary>
        public void Restart()
        {
            _restart.UnionWith(_pending.Keys);
            _restart.UnionWith(_done);
            _pending.Clear();
            _done.Clear();
        }

        /// <summary>The clients <see cref="Restart"/> set aside, now to begin again; empties the set.</summary>
        public int[] TakeRestarts()
        {
            int[] clientIds = new int[_restart.Count];
            _restart.CopyTo(clientIds);
            _restart.Clear();
            return clientIds;
        }

        public bool HasRestarts => _restart.Count > 0;

        /// <summary>
        /// Hands out at most <paramref name="max"/> chunks in all, taking turns between clients so one large station
        /// doesn't hold another client's back. A client whose queue empties is done. Returns how many were sent.
        /// </summary>
        public int Drain(int max, Action<int, Vector2Int> send)
        {
            if (send == null || max <= 0 || _pending.Count == 0)
                return 0;

            int sent = 0;
            while (sent < max && _pending.Count > 0)
            {
                _clientIds.Clear();
                _clientIds.AddRange(_pending.Keys);
                _clientIds.Sort();
                int clientId = _clientIds[_next++ % _clientIds.Count];
                Queue<Vector2Int> queue = _pending[clientId];
                send(clientId, queue.Dequeue());
                sent++;
                if (queue.Count == 0)
                {
                    _pending.Remove(clientId);
                    _done.Add(clientId);
                }
            }

            return sent;
        }
    }
}
