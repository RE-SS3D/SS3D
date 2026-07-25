using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Shared helper for the debug/emote animation console commands (Posture/Limp/HoldPose/Flinch/
    /// ThrowAnim/Emote) to resolve the calling client's own HumanoidAnimatorController. These
    /// commands are all CommandType.Client, so Perform/CheckArgs run locally on the caller's machine
    /// with no NetworkConnection available - EntitySubSystem.TryGetLocalEntity's IsOwner-based lookup
    /// is what makes "my own entity" resolvable from that context.
    /// </summary>
    internal static class LocalHumanoidAnimatorResolver
    {
        public static bool TryGetLocalHumanoidAnimatorController(out HumanoidAnimatorController controller)
        {
            controller = null;

            if (!SubSystems.Get<EntitySubSystem>().TryGetLocalEntity(out Entity entity))
            {
                return false;
            }

            controller = entity.GetComponent<HumanoidAnimatorController>();
            return controller != null;
        }
    }
}
