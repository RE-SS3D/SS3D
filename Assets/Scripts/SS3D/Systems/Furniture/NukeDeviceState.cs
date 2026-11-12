using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// The nuclear device's state and its rules (<c>antagonist-content.md</c> §6: a timed charge with a
    /// disk-loaded precondition). Pure, so the steps are tested without a network; <see cref="Nuke"/> keeps one
    /// in sync vars and asks it what each interaction may do.
    /// </summary>
    /// <remarks>
    /// The loop: load the disk, arm (countdown starts), and only the two-step defuse stops it: open the wiring
    /// panel with a screwdriver, then cut the wires. Pulling the disk out after arming does not stop the clock.
    /// A defuse leaves the device ready to re-arm once a disk is loaded again.
    /// </remarks>
    public readonly struct NukeDeviceState
    {
        public readonly bool DiskLoaded;
        public readonly bool Armed;
        public readonly bool PanelOpen;
        /// <summary>The device went off; nothing more happens to it.</summary>
        public readonly bool Spent;
        /// <summary>Server time of detonation while <see cref="Armed"/>.</summary>
        public readonly double Deadline;

        public NukeDeviceState(bool diskLoaded, bool armed, bool panelOpen, bool spent, double deadline)
        {
            DiskLoaded = diskLoaded;
            Armed = armed;
            PanelOpen = panelOpen;
            Spent = spent;
            Deadline = deadline;
        }

        public bool CanLoadDisk => !DiskLoaded && !Spent;

        public bool CanEjectDisk => DiskLoaded && !Spent;

        public bool CanArm => DiskLoaded && !Armed && !Spent;

        public bool CanOpenPanel(double now) => Ticking(now) && !PanelOpen;

        public bool CanCutWires(double now) => Ticking(now) && PanelOpen;

        public bool ShouldDetonate(double now) => Armed && !Spent && now >= Deadline;

        /// <summary>Seconds left on the clock; zero when not armed.</summary>
        public float Remaining(double now) => Armed ? Mathf.Max(0f, (float)(Deadline - now)) : 0f;

        public NukeDeviceState WithDisk(bool loaded) => new(loaded, Armed, PanelOpen, Spent, Deadline);

        public NukeDeviceState ArmedAt(double now, float countdownSeconds) =>
            new(DiskLoaded, true, false, Spent, now + countdownSeconds);

        public NukeDeviceState WithPanelOpen() => new(DiskLoaded, Armed, true, Spent, Deadline);

        /// <summary>Cut wires: the clock stops and the panel closes; the disk stays where it is.</summary>
        public NukeDeviceState Defused() => new(DiskLoaded, false, false, Spent, 0d);

        public NukeDeviceState Detonated() => new(DiskLoaded, false, false, true, Deadline);

        private bool Ticking(double now) => Armed && !Spent && now < Deadline;

        /// <summary>Seconds left when the station-wide alarm sounds.</summary>
        public const float AlarmSeconds = 10f;

        /// <summary>Gap between countdown beeps: every 2 s, every second in the last 30, twice a second in the last 10.</summary>
        public static float TickInterval(float remaining) => remaining > 30f ? 2f : remaining > AlarmSeconds ? 1f : 0.5f;

        /// <summary>The final alarm is due: armed, not yet sounded, and inside <see cref="AlarmSeconds"/>.</summary>
        public bool AlarmDue(double now, bool alarmSent) => Armed && !Spent && !alarmSent && Remaining(now) <= AlarmSeconds;

        /// <summary>"m:ss", rounded up so the readout never shows 0:00 before the blast.</summary>
        public static string FormatTime(float seconds)
        {
            int whole = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{whole / 60}:{whole % 60:00}";
        }
    }
}
