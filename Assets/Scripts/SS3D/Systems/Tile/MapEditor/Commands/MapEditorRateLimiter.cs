using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    /// <summary>
    /// Per-client token bucket on map edit operations, so one player on an open-build server cannot
    /// flood the server. A full bucket covers a large drag-rectangle; sustained spam is throttled.
    /// </summary>
    public sealed class MapEditorRateLimiter
    {
        private readonly double _capacity;
        private readonly double _refillPerSecond;
        private readonly Dictionary<int, (double Tokens, double Time)> _buckets = new();

        public MapEditorRateLimiter(double capacity = 4000, double refillPerSecond = 400)
        {
            _capacity = capacity;
            _refillPerSecond = refillPerSecond;
        }

        /// <summary>Spends <paramref name="cost"/> tokens for <paramref name="clientId"/>; false (and nothing spent) when short.</summary>
        public bool TryConsume(int clientId, int cost, double now)
        {
            (double tokens, double time) = _buckets.TryGetValue(clientId, out var bucket) ? bucket : (_capacity, now);
            tokens = Math.Min(_capacity, tokens + Math.Max(0, now - time) * _refillPerSecond);

            bool allowed = cost <= tokens;
            if (allowed)
                tokens -= cost;

            _buckets[clientId] = (tokens, now);
            return allowed;
        }

        public void Forget(int clientId) => _buckets.Remove(clientId);
    }
}
