using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>Spawns a mindless Human with frozen controls, wearing JumpsuitSecurity, for melee, armor and interaction tests.</summary>
    public class SpawnDummyCommand : Command
    {
        private const float SpawnDistance = 2f;

        public override string Name => "spawndummy";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Spawn a standing combat dummy in front of you; target it with nearest";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override string Example => "";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            Entity entity = context.CallerEntity;
            if (entity == null)
            {
                return CommandResult.Error("you have no body to spawn a dummy in front of");
            }

            Transform transform = entity.transform;
            Vector3 spawnPosition = transform.position + transform.forward * SpawnDistance;
            Quaternion spawnRotation = Quaternion.LookRotation(-transform.forward, Vector3.up);

            Entity dummy = SubSystems.Get<EntitySubSystem>().ServerSpawnCombatDummy(spawnPosition, spawnRotation);
            if (dummy == null)
            {
                return CommandResult.Error("failed to spawn combat dummy (no human prefab configured)");
            }

            return CommandResult.Success($"Combat dummy spawned at {spawnPosition} (wearing JumpsuitSecurity)");
        }
    }
}
