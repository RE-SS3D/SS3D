using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Pure rules of the Syndicate bomb (explosives-destruction.md §6). The timer is set before arming and
    /// never goes below <see cref="MinTimerSeconds"/>. A wrench bolts the bomb to the floor so it can't be
    /// dragged or blown away; once armed the bolts lock, so it can still be bolted down but not unbolted.
    /// Defusing is the nuke's two steps: a screwdriver opens the wiring panel, then wirecutters cut the wires.
    /// A defused bomb can be re-armed.
    /// </summary>
    public readonly struct SyndicateBombState
    {
        public const float MinTimerSeconds = 90f;
        public const float MaxTimerSeconds = 600f;
        public const float TimerStepSeconds = 30f;

        public readonly bool Armed;
        public readonly bool PanelOpen;
        public readonly bool Anchored;
        public readonly bool Spent;
        public readonly double Deadline;
        public readonly float TimerSeconds;

        public SyndicateBombState(bool armed, bool panelOpen, bool anchored, bool spent, double deadline, float timerSeconds)
        {
            Armed = armed;
            PanelOpen = panelOpen;
            Anchored = anchored;
            Spent = spent;
            Deadline = deadline;
            TimerSeconds = ClampTimer(timerSeconds);
        }

        public static SyndicateBombState Fresh => new(false, false, false, false, 0d, MinTimerSeconds);

        public static float ClampTimer(float seconds) => Mathf.Clamp(seconds, MinTimerSeconds, MaxTimerSeconds);

        public bool CanSetTimer => !Armed && !Spent;

        public bool CanRaiseTimer => CanSetTimer && TimerSeconds < MaxTimerSeconds;

        public bool CanLowerTimer => CanSetTimer && TimerSeconds > MinTimerSeconds;

        public bool CanArm => !Armed && !Spent;

        public bool CanAnchor => !Anchored && !Spent;

        /// <summary>Armed bolts are locked: the whole point of wrenching it down.</summary>
        public bool CanUnanchor => Anchored && !Armed && !Spent;

        /// <summary>Only a loose bomb can be dragged (or carried off by the wind).</summary>
        public bool CanMove => !Anchored && !Spent;

        public bool CanOpenPanel(double now) => Ticking(now) && !PanelOpen;

        public bool CanCutWires(double now) => Ticking(now) && PanelOpen;

        public bool ShouldDetonate(double now) => Armed && !Spent && now >= Deadline;

        public float Remaining(double now) => Armed ? Mathf.Max(0f, (float)(Deadline - now)) : TimerSeconds;

        public SyndicateBombState WithTimer(float seconds) => new(Armed, PanelOpen, Anchored, Spent, Deadline, seconds);

        public SyndicateBombState WithAnchored(bool anchored) => new(Armed, PanelOpen, anchored, Spent, Deadline, TimerSeconds);

        public SyndicateBombState ArmedAt(double now) => new(true, false, Anchored, Spent, now + TimerSeconds, TimerSeconds);

        public SyndicateBombState WithPanelOpen() => new(Armed, true, Anchored, Spent, Deadline, TimerSeconds);

        public SyndicateBombState Defused() => new(false, false, Anchored, Spent, 0d, TimerSeconds);

        public SyndicateBombState Detonated() => new(false, false, Anchored, true, Deadline, TimerSeconds);

        /// <summary>Seconds between countdown beeps: steady, then faster in the last 30 and 10 seconds.</summary>
        public static float BeepInterval(float remaining) => remaining > 30f ? 1f : remaining > 10f ? 0.5f : 0.25f;

        private bool Ticking(double now) => Armed && !Spent && now < Deadline;
    }
}
