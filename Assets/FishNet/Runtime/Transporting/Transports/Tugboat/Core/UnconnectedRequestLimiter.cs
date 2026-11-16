using System;
using System.Collections.Generic;
using System.Net;

namespace FishNet.Transporting.Tugboat.Server
{
    /// <summary>
    /// SS3D patch (tech-debt P1.8): rate limit for unconnected status queries. A token bucket per source address plus a
    /// global bucket, so a flood of spoofed queries can't turn the server into a reflector (replies are larger than
    /// requests). Thread-safe: LiteNetLib may deliver unconnected packets on more than one receive thread.
    /// </summary>
    public sealed class UnconnectedRequestLimiter
    {
        private struct Bucket
        {
            public double Tokens;
            public double LastSeconds;
        }

        private readonly object _lock = new();
        private readonly Dictionary<IPAddress, Bucket> _perAddress = new();
        private readonly double _addressBurst;
        private readonly double _addressPerSecond;
        private readonly double _globalBurst;
        private readonly double _globalPerSecond;
        private readonly int _maxTrackedAddresses;
        private Bucket _global;

        /// <param name="addressBurst">Queries one address may send at once (one menu refresh sends five; a shared NAT sends more).</param>
        /// <param name="addressPerSecond">Steady queries per second per address.</param>
        /// <param name="globalBurst">Queries the whole server answers at once.</param>
        /// <param name="globalPerSecond">Steady queries per second for the whole server.</param>
        /// <param name="maxTrackedAddresses">Past this many addresses the table is cleared, so memory stays bounded.</param>
        public UnconnectedRequestLimiter(double addressBurst = 16, double addressPerSecond = 4, double globalBurst = 200,
            double globalPerSecond = 100, int maxTrackedAddresses = 4096)
        {
            _addressBurst = addressBurst;
            _addressPerSecond = addressPerSecond;
            _globalBurst = globalBurst;
            _globalPerSecond = globalPerSecond;
            _maxTrackedAddresses = maxTrackedAddresses;
            _global = new Bucket { Tokens = globalBurst, LastSeconds = double.NaN };
        }

        /// <summary>True when a query from <paramref name="address"/> at <paramref name="nowSeconds"/> may be answered.</summary>
        public bool TryAcquire(IPAddress address, double nowSeconds)
        {
            if (address == null)
            {
                return false;
            }

            lock (_lock)
            {
                if (!_perAddress.TryGetValue(address, out Bucket bucket))
                {
                    if (_perAddress.Count >= _maxTrackedAddresses)
                    {
                        _perAddress.Clear();
                    }

                    bucket = new Bucket { Tokens = _addressBurst, LastSeconds = nowSeconds };
                }

                Refill(ref bucket, nowSeconds, _addressBurst, _addressPerSecond);
                Refill(ref _global, nowSeconds, _globalBurst, _globalPerSecond);

                bool allowed = bucket.Tokens >= 1 && _global.Tokens >= 1;
                if (allowed)
                {
                    bucket.Tokens -= 1;
                    _global.Tokens -= 1;
                }

                _perAddress[address] = bucket;
                return allowed;
            }
        }

        private static void Refill(ref Bucket bucket, double nowSeconds, double burst, double perSecond)
        {
            if (!double.IsNaN(bucket.LastSeconds))
            {
                double elapsed = Math.Max(0, nowSeconds - bucket.LastSeconds);
                bucket.Tokens = Math.Min(burst, bucket.Tokens + (elapsed * perSecond));
            }

            bucket.LastSeconds = nowSeconds;
        }
    }
}
