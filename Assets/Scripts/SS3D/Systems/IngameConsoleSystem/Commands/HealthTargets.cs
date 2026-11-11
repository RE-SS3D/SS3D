using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>Shared lookup for commands that act on a target's health.</summary>
    public static class HealthTargets
    {
        public static bool TryGetHealth(CommandTarget target, out HumanHealthController health, out CommandResult error)
        {
            if (target.TryGetComponent(out health))
            {
                error = default;
                return true;
            }

            error = CommandResult.Error($"{target.DisplayName} has no health (a ghost, or not a body)");
            return false;
        }
    }
}
