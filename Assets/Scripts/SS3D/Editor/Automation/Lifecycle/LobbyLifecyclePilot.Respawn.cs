using System.Linq;
using FishNet;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Rounds;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.Automation
{
    public static partial class LobbyLifecyclePilot
    {
        private const string RespawnCleanupMode = "respawn-cleanup";
        private static Human _respawnCorpse;

        [MenuItem("SS3D/Testing/Run Respawn Cleanup Regressions")]
        public static void RunRespawnCleanupRegressions() => Start(RespawnCleanupMode);

        private static Step CleanupDeathStep() => new()
        {
            Name = "Death preserves the corpse while respawn is refused",
            Enter = () =>
            {
                ServerSettings settings = Rounds().SettingsForServer;
                settings.RespawnCooldownSeconds = 8;
                Rounds().ApplyServerSettingsForTesting(settings);
                _respawnCorpse = (Human)SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local());
                _respawnCorpse.Kill();
                Require(!SubSystems.Get<EntitySubSystem>().TryRespawn(InstanceFinder.ClientManager.Connection, out _),
                    "Cooldown refuses the respawn");
                Require(_respawnCorpse != null && _respawnCorpse.IsSpawned,
                    "Refused respawn keeps the previous corpse");
                Settle(0.5);
            },
            Until = () => Settled() && LocalEntityIsGhost(),
        };

        private static Step CleanupRespawnStep() => new()
        {
            Name = "Successful respawn removes only the player's previous corpse",
            Enter = () =>
            {
                ServerSettings settings = Rounds().SettingsForServer;
                settings.RespawnCooldownSeconds = 0;
                Rounds().ApplyServerSettingsForTesting(settings);
                Require(SubSystems.Get<EntitySubSystem>().TryRespawn(InstanceFinder.ClientManager.Connection, out _),
                    "Respawn succeeds after releasing the cooldown");
                Settle(2);
            },
            Until = () => Settled() && Spawned() && !LocalEntityIsGhost() && _respawnCorpse == null,
            Verify = () => Require(Object.FindObjectsByType<Human>(FindObjectsSortMode.None).Length == 1,
                "Repeated sandbox respawns leave one living Human and no previous corpses"),
        };

        private static Step[] BuildRespawnCleanupSteps() => BuildSandboxSteps().Take(4).Concat(new Step[]
        {
            CleanupDeathStep(), CleanupRespawnStep(), CleanupDeathStep(), CleanupRespawnStep(),
        }).ToArray();
    }
}
