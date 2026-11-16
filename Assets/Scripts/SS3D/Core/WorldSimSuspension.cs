using System;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Server: one ref-counted pause for the per-tick world simulations (atmos, electricity) while the station is being
    /// replaced: map load, clear and import (tech-debt P2.2, plan Decision 10). Each operation takes a
    /// <see cref="Scope"/> and disposes it when done; the sims stay paused until every scope is closed, so a load inside
    /// another load can't resume them early. Opening the Map Editor takes no scope. A later admin "freeze world" control
    /// would take one too.
    /// </summary>
    public static class WorldSimSuspension
    {
        private static int _depth;

        /// <summary>True while at least one <see cref="Scope"/> is open. Sims check it every tick.</summary>
        public static bool IsSuspended => _depth > 0;

        /// <summary>How many scopes are open (diagnostics and tests).</summary>
        public static int Depth => _depth;

        /// <summary>Opens a scope; dispose it (ideally in a <c>finally</c>) to release it. Disposing twice is harmless.</summary>
        public static Scope Begin(string reason)
        {
            _depth++;
            return new Scope(reason);
        }

        public sealed class Scope : IDisposable
        {
            private bool _disposed;

            internal Scope(string reason) => Reason = reason;

            /// <summary>What took the scope ("map load", "map import"), for logs.</summary>
            public string Reason { get; }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _depth = Math.Max(0, _depth - 1);
            }
        }

        /// <summary>Enter Play Mode without a domain reload must not inherit a scope a previous session leaked.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetForNewSession() => _depth = 0;
    }
}
