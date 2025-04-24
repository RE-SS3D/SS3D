using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Manages all minds in the game.
    /// </summary>
    public class MindSubSystem : NetworkSubSystem
    {
        [SerializeField]
        private GameObject _mindPrefab;

        [SerializeField]
        private Mind _emptyMind;

        [SyncObject]
        private readonly SyncList<Mind> _spawnedMinds = new();

        public Mind EmptyMind => _emptyMind;

        /// <summary>
        /// Tried to get the player's mind.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="mind"></param>
        /// <returns></returns>
        public bool TryGetMind(Player player, out Mind mind)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            // todo inspect do we really need to GetPlayer when we pass a player already?
            Player actualPlayer = playerSystem.GetPlayer(player.Owner);

            mind = _spawnedMinds.Find(spawnedMind => spawnedMind.player == actualPlayer);

            if (mind != null)
            {
                return true;
            }

            mind = null;
            return false;
        }

        /// <summary>
        /// Returns if a user has a mind or not.
        /// </summary>
        /// <param name="player</param>
        /// <returns></returns>
        public bool HasMind(Player player)
        {
            Mind mind = _spawnedMinds.Find(spawnedMind => spawnedMind.player == player);

            return mind != null;
        }

        /// <summary>
        /// Tries to create a mind for a user.
        /// </summary>
        /// <param name="player</param>
        /// <param name="createdMind"></param>
        /// <returns></returns>
        [Server]
        public bool TryCreateMind(Player player, out Mind createdMind)
        {
            if (HasMind(player))
            {
                createdMind = null;
                return false;
            }

            Mind mind = Instantiate(_mindPrefab).GetComponent<Mind>();
            ServerManager.Spawn(mind.GameObject, player.Owner);

            mind.SetPlayer(player);

            createdMind = mind;
            return true;
        }

        /// <summary>
        /// Asks the server to execute a mind swap.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        [ServerRpc]
        public void CmdSwapMinds(Entity origin, Entity target, NetworkConnection networkConnection = null)
        {
            SwapMinds(origin, target);
        }

        /// <summary>
        /// Executes a mind swap.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        [Server]
        public void SwapMinds(Entity origin, Entity target)
        {
            Mind originMind = origin.Mind;
            Mind targetMind = target.Mind;

            origin.SetMind(targetMind);
            target.SetMind(originMind);
        }
    }
}