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
    public class MindSystem : NetworkSystem
    {
        [SerializeField]
        private Mind _mindPrefab;

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
        public bool TryGetMind(Soul player, out Mind mind)
        {
            PlayerControlSystem playerControlSystem = SystemLocator.Get<PlayerControlSystem>();
            Soul soul = playerControlSystem.GetSoul(player.Owner);

            mind = _spawnedMinds.Find(mind => mind.Soul == soul);

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
        /// <param name="soul"></param>
        /// <returns></returns>
        public bool HasMind(Soul soul)
        {
            Mind mind = _spawnedMinds.Find(mind => mind.Soul == soul);

            return mind != null;
        }

        /// <summary>
        /// Tries to create a mind for an user.
        /// </summary>
        /// <param name="soul"></param>
        /// <param name="createdMind"></param>
        /// <returns></returns>
        [Server]
        public bool TryCreateMind(Soul soul, out Mind createdMind)
        {
            if (HasMind(soul))
            {
                createdMind = null;
                return false;
            }

            Mind mind = Instantiate(_mindPrefab);
            ServerManager.Spawn(mind.GameObjectCache, soul.Owner);

            mind.SetSoul(soul);

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
        private void SwapMinds(Entity origin, Entity target)
        {
            Mind originSoul = origin.Mind;
            Mind targetSoul = target.Mind;

            origin.SetMind(targetSoul);
            target.SetMind(originSoul);
        }
    }
}