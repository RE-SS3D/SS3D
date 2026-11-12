using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Timing;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// One clock for the fixed-rate world simulations (tech-debt P6.1). FishNet's <see cref="TimeManager.OnTick"/> drives
    /// it on every peer that has a network running, and it feeds named <see cref="SimulationChannel"/>s that fire at their
    /// own interval and keep the remainder, so a 5 Hz channel fires exactly five times a second on a 30 Hz tick.
    /// Channels that <see cref="SimulationChannel.PausedByWorldSuspension"/> stop while a
    /// <see cref="WorldSimSuspension"/> scope is open: the clock pauses channels, not systems.
    /// </summary>
    public static class SimulationClock
    {
        private static readonly Dictionary<string, SimulationChannel> Channels = new();
        private static readonly List<SimulationChannel> Order = new();
        private static TimeManager _timeManager;

        /// <summary>Every channel, in creation order (budget logs).</summary>
        public static IReadOnlyList<SimulationChannel> All => Order;

        /// <summary>
        /// Returns the channel called <paramref name="name"/>, creating it the first time. Every caller of one name must
        /// agree on its interval and pausing; a mismatch is a bug, so it throws.
        /// </summary>
        public static SimulationChannel GetOrCreate(string name, float intervalSeconds, bool pausedByWorldSuspension)
        {
            if (Channels.TryGetValue(name, out SimulationChannel existing))
            {
                if (!Mathf.Approximately(existing.IntervalSeconds, intervalSeconds) || existing.PausedByWorldSuspension != pausedByWorldSuspension)
                {
                    throw new InvalidOperationException($"Simulation channel \"{name}\" already exists with a different interval or pausing");
                }

                return existing;
            }

            var channel = new SimulationChannel(name, intervalSeconds, pausedByWorldSuspension);
            Channels.Add(name, channel);
            Order.Add(channel);
            return channel;
        }

        /// <summary>Advances every channel by <paramref name="seconds"/>. The network tick calls this; tests call it directly.</summary>
        public static void Advance(double seconds)
        {
            bool suspended = WorldSimSuspension.IsSuspended;
            for (int i = 0; i < Order.Count; i++)
            {
                Order[i].Advance(seconds, suspended);
            }
        }

        /// <summary>Hooks the clock to the running <see cref="TimeManager"/>; subscribing to a channel calls this.</summary>
        internal static void EnsureAttached()
        {
            TimeManager timeManager = InstanceFinder.TimeManager;
            if (timeManager == _timeManager)
            {
                return;
            }

            if (_timeManager != null)
            {
                _timeManager.OnTick -= HandleTick;
            }

            _timeManager = timeManager;
            if (_timeManager != null)
            {
                _timeManager.OnTick += HandleTick;
            }
        }

        private static void HandleTick() => Advance(_timeManager.TickDelta);

        /// <summary>Enter Play Mode without a domain reload must not keep the last session's channels or subscribers.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetForNewSession()
        {
            if (_timeManager != null)
            {
                _timeManager.OnTick -= HandleTick;
            }

            _timeManager = null;
            Channels.Clear();
            Order.Clear();
        }
    }

    /// <summary>A fixed-rate stream of ticks from <see cref="SimulationClock"/>. Subscribers run newest first; don't depend on the order.</summary>
    public sealed class SimulationChannel
    {
        // Ticks are float sums of 1/30 s; without it, five of them can fall a hair short of 1/6 s and the fire slips a tick.
        private const double Epsilon = 1e-6;

        private readonly List<Action> _subscribers = new();
        private readonly ProfilerMarker _marker;
        private double _elapsed;
        private bool _wasSuspended;

        internal SimulationChannel(string name, float intervalSeconds, bool pausedByWorldSuspension)
        {
            if (intervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), intervalSeconds, "must be positive");
            }

            Name = name;
            _marker = new ProfilerMarker($"SS3D.Clock.{name}");
            IntervalSeconds = intervalSeconds;
            PausedByWorldSuspension = pausedByWorldSuspension;
        }

        public string Name { get; }

        public float IntervalSeconds { get; }

        /// <summary>True when an open <see cref="WorldSimSuspension"/> scope stops this channel.</summary>
        public bool PausedByWorldSuspension { get; }

        /// <summary>A pause owned by the channel's system (for example the atmos debug pause). Time doesn't build up while set.</summary>
        public bool Paused { get; set; }

        /// <summary>Times this channel has fired.</summary>
        public long TickCount { get; private set; }

        /// <summary>Raised on the first advance after a world suspension that paused this channel closes, before it fires again.</summary>
        public event Action Resumed;

        /// <summary>Adds <paramref name="onTick"/> once; subscribing it again (a restarted server) is a no-op.</summary>
        public void Subscribe(Action onTick)
        {
            SimulationClock.EnsureAttached();
            if (!_subscribers.Contains(onTick))
            {
                _subscribers.Add(onTick);
            }
        }

        public void Unsubscribe(Action onTick) => _subscribers.Remove(onTick);

        /// <summary>Drops the remainder, so the next tick is a full interval away (a system restarting its simulation).</summary>
        public void ResetPhase() => _elapsed = 0d;

        internal void Advance(double seconds, bool worldSuspended)
        {
            if (PausedByWorldSuspension)
            {
                if (worldSuspended)
                {
                    _wasSuspended = true;
                    return;
                }

                if (_wasSuspended)
                {
                    _wasSuspended = false;
                    Resumed?.Invoke();
                }
            }

            if (Paused)
            {
                return;
            }

            _elapsed += seconds;
            while (_elapsed + Epsilon >= IntervalSeconds)
            {
                _elapsed -= IntervalSeconds;
                TickCount++;
                Fire();
            }
        }

        // The marker groups a fire's cost and GC allocations in the Profiler (tech-debt P6.2: a tick should allocate nothing).
        private void Fire()
        {
            using ProfilerMarker.AutoScope scope = _marker.Auto();
            // Backwards, so a subscriber may unsubscribe itself (or others) mid-fire without a per-tick copy.
            for (int i = _subscribers.Count - 1; i >= 0; i--)
            {
                if (i < _subscribers.Count)
                {
                    _subscribers[i].Invoke();
                }
            }
        }
    }
}
