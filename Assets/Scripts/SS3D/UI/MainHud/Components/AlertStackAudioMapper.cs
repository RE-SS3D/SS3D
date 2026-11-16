using System;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Detects a newly-appearing hazard between two <see cref="AlertStackState"/> snapshots
    /// (audio.md §6). "New" means a hazard going from <see cref="AlertSeverity.None"/> to any other
    /// severity — an escalation already showing (Warning → Critical) is not a new alert, it's the
    /// same chip getting worse, so it doesn't re-trigger the cue.
    /// </summary>
    public static class AlertStackAudioMapper
    {
        public static bool HasNewAlert(AlertStackState previous, AlertStackState current)
        {
            foreach (AlertHazard hazard in (AlertHazard[])Enum.GetValues(typeof(AlertHazard)))
            {
                // A status the player chose, not a danger, so it stays quiet.
                if (hazard != AlertHazard.Invincible
                    && previous[hazard] == AlertSeverity.None && current[hazard] != AlertSeverity.None)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
