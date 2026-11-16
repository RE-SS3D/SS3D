using System;
using System.Globalization;
using FishNet.Managing.Server;
using SS3D.Data.Management;
using SS3D.Data.Persistence;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace SS3D.Systems.Rounds
{
    /// <summary>One line of <c>reset-health.jsonl</c>: what was left after a round's station was unloaded.</summary>
    [Serializable]
    public sealed class ResetHealthEntry
    {
        public string timestamp;
        public int roundId;
        public int players;
        public int networkObjects;
        public int gameObjects;
        public long managedHeapBytes;
        public long totalAllocatedBytes;
        /// <summary>Placed tile objects still in the scene after unload; anything above 0 is a leak.</summary>
        public int tileObjects;
        /// <summary>Items still in the scene after unload; anything above 0 is a leak.</summary>
        public int items;
    }

    /// <summary>
    /// Sandbox observability (<c>2026-11_sandbox-server.md</c> P4): a <c>reset-health.jsonl</c> line in
    /// <c>ServerMeta</c>, beside the round history, after every station unload. Bug reports, once here too,
    /// are <see cref="SS3D.Systems.BugReports.BugReportIntake"/> (tech-debt P6.5).
    /// </summary>
    public static class RoundObservability
    {
        /// <summary>Server: writes the reset-health line once the station is unloaded and the crew despawned.</summary>
        public static void AppendResetHealth(object logSource, int roundId, ServerManager serverManager)
        {
            ResetHealthEntry entry = new()
            {
                timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                roundId = roundId,
                players = serverManager != null ? serverManager.Clients.Count : 0,
                networkObjects = serverManager != null ? serverManager.Objects.Spawned.Count : 0,
                gameObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                managedHeapBytes = GC.GetTotalMemory(false),
                totalAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                tileObjects = Object.FindObjectsByType<PlacedTileObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                items = Object.FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
            };
            if (!LocalStorage.AppendJsonlLine(PersistencePaths.ResetHealth, JsonUtility.ToJson(entry)))
                Log.Warning(logSource, "Could not write reset-health", Logs.ServerOnly);
            Log.Information(logSource,
                "Round {roundId} reset health: {networkObjects} network objects, {gameObjects} game objects, {tileObjects} tile objects, {items} items, {heap} B heap",
                Logs.ServerOnly, entry.roundId, entry.networkObjects, entry.gameObjects, entry.tileObjects, entry.items, entry.managedHeapBytes);
        }
    }
}
