using System;
using System.Reflection;
using NUnit.Framework;
using SS3D.Core;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P6.1: one clock, fixed-rate channels that keep the remainder, paused by world suspension.</summary>
    public sealed class SimulationClockTests
    {
        private const double NetworkTick = 1d / 30d;

        [SetUp]
        [TearDown]
        public void Reset()
        {
            Invoke(typeof(SimulationClock), "ResetForNewSession");
            Invoke(typeof(WorldSimSuspension), "ResetForNewSession");
        }

        [Test]
        public void FiveHertzChannel_FiresFiveTimesPerSecondOfNetworkTicks()
        {
            SimulationChannel channel = SimulationClock.GetOrCreate("test", 0.2f, pausedByWorldSuspension: false);
            int fired = 0;
            channel.Subscribe(() => fired++);

            AdvanceTicks(30 * 10);

            Assert.AreEqual(50, fired, "ten seconds at 30 Hz is fifty 5 Hz ticks: none lost to float drift");
        }

        [Test]
        public void Remainder_CarriesOver()
        {
            SimulationChannel channel = SimulationClock.GetOrCreate("test", 1f, pausedByWorldSuspension: false);
            int fired = 0;
            channel.Subscribe(() => fired++);

            SimulationClock.Advance(0.7d);
            SimulationClock.Advance(0.7d);
            Assert.AreEqual(1, fired);

            // 1.4 s so far, with 0.4 s carried: another 0.6 s reaches the second tick. Resetting to zero would need 1 s.
            SimulationClock.Advance(0.6d);
            Assert.AreEqual(2, fired);
        }

        [Test]
        public void ChannelsSharingTheClock_KeepTheirOwnRates()
        {
            int fast = 0;
            int slow = 0;
            SimulationClock.GetOrCreate("fast", 0.2f, pausedByWorldSuspension: false).Subscribe(() => fast++);
            SimulationClock.GetOrCreate("slow", 1f, pausedByWorldSuspension: false).Subscribe(() => slow++);

            AdvanceTicks(60);

            Assert.AreEqual(10, fast);
            Assert.AreEqual(2, slow);
        }

        [Test]
        public void GetOrCreate_ReturnsTheSameChannel_AndRejectsADifferentRate()
        {
            SimulationChannel first = SimulationClock.GetOrCreate("atmos", 0.2f, pausedByWorldSuspension: true);
            Assert.AreSame(first, SimulationClock.GetOrCreate("atmos", 0.2f, pausedByWorldSuspension: true));
            Assert.Throws<InvalidOperationException>(() => SimulationClock.GetOrCreate("atmos", 1f, pausedByWorldSuspension: true));
        }

        [Test]
        public void WorldSuspension_PausesOnlyChannelsThatAskForIt_AndRaisesResumedOnce()
        {
            int paused = 0;
            int unpaused = 0;
            int resumed = 0;
            SimulationChannel sim = SimulationClock.GetOrCreate("sim", 0.2f, pausedByWorldSuspension: true);
            sim.Subscribe(() => paused++);
            sim.Resumed += () => resumed++;
            SimulationClock.GetOrCreate("health", 0.2f, pausedByWorldSuspension: false).Subscribe(() => unpaused++);

            using (WorldSimSuspension.Begin("map load"))
            {
                AdvanceTicks(30);
            }

            Assert.AreEqual(0, paused, "no ticks while the station is replaced");
            Assert.AreEqual(5, unpaused);
            Assert.AreEqual(0, resumed, "resume is seen on the next advance, not when the scope closes");

            AdvanceTicks(30);
            Assert.AreEqual(5, paused, "time spent suspended doesn't come back as a burst of ticks");
            Assert.AreEqual(1, resumed);
        }

        [Test]
        public void OwnPause_BuildsUpNoTime()
        {
            SimulationChannel channel = SimulationClock.GetOrCreate("test", 0.2f, pausedByWorldSuspension: false);
            int fired = 0;
            channel.Subscribe(() => fired++);

            channel.Paused = true;
            AdvanceTicks(30);
            channel.Paused = false;
            AdvanceTicks(5);

            Assert.AreEqual(0, fired);
        }

        [Test]
        public void Subscribe_IsIdempotent_AndASubscriberMayLeaveMidFire()
        {
            SimulationChannel channel = SimulationClock.GetOrCreate("test", 0.2f, pausedByWorldSuspension: false);
            int fired = 0;
            int other = 0;
            void Once()
            {
                fired++;
                channel.Unsubscribe(Once);
            }

            channel.Subscribe(() => other++);
            channel.Subscribe(Once);
            channel.Subscribe(Once);

            AdvanceTicks(12);

            Assert.AreEqual(1, fired);
            Assert.AreEqual(2, other);
            Assert.AreEqual(2, channel.TickCount);
        }

        private static void AdvanceTicks(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                SimulationClock.Advance(NetworkTick);
            }
        }

        private static void Invoke(Type type, string method) =>
            type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
}
