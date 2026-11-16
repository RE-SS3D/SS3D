using Coimbra;
using FishNet;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Networking
{
    /// <summary>
    /// Single networked hub NetworkObject for world/session <see cref="NetworkSubSystem"/>s.
    /// Prefab is edit-time owned (SS3D/Bootstrap/Rebuild NetworkSystemsHub Prefab); server spawns
    /// on Online. Scene Boot/Game no longer place per-system GameObjects.
    /// </summary>
    public sealed class NetworkSystemsHub : NetworkActor
    {
        public const string ResourcesPrefabPath = "NetworkSystemsHub";

        public static NetworkSystemsHub Instance { get; private set; }

        protected override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Log.Information(this, "NetworkSystemsHub started on server", Logs.ServerOnly);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Instance = this;
            Log.Information(this, "NetworkSystemsHub started on client", Logs.Generic);
        }

        protected override void OnDestroyed()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnDestroyed();
        }

        /// <summary>
        /// Spawns the hub prefab on the server if none exists. Safe to call after Online;
        /// no-ops when a scene-placed or already-spawned hub is present.
        /// </summary>
        public static void EnsureSpawned()
        {
            if (!InstanceFinder.IsServer)
            {
                return;
            }

            if (Instance != null)
            {
                return;
            }

            NetworkSystemsHub existing = Object.FindObjectOfType<NetworkSystemsHub>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(ResourcesPrefabPath);
            if (prefab == null)
            {
                Log.Warning(typeof(NetworkSystemsHub),
                    "Resources/{path} prefab missing — hub dual-run skipped until prefab is created (SS3D/Bootstrap/Create NetworkSystemsHub Prefab).",
                    Logs.Important,
                    ResourcesPrefabPath);
                return;
            }

            GameObject instance = Object.Instantiate(prefab);
            NetworkObject nob = instance.GetComponent<NetworkObject>();
            if (nob == null)
            {
                Log.Error(typeof(NetworkSystemsHub), "NetworkSystemsHub prefab lacks NetworkObject", Logs.Important);
                instance.Dispose(true);
                return;
            }

            InstanceFinder.ServerManager.Spawn(nob);
            Log.Information(typeof(NetworkSystemsHub), "Spawned NetworkSystemsHub", Logs.ServerOnly);
        }
    }
}
