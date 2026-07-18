using SS3D.Logging;

namespace SS3D.Systems.Testing
{
    /// <summary>
    /// Structured log lines the multiplayer test harness (Testing/multiplayer/) tails to detect
    /// readiness/completion instead of sleeping or polling scene state. Only emitted while
    /// <see cref="AutomationSubSystem"/> is actively running a script, so this has no footprint on
    /// normal play.
    /// </summary>
    public static class TestSignal
    {
        public static void Emit(object sender, string signal, string payload = "")
        {
            Log.Information(sender, "Test signal {signal} {payload}", Logs.Testing, signal, payload);
        }
    }
}
