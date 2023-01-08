using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems
{
    /// <summary>
    /// Manages all minds in the game.
    /// </summary>
    public class MindSystem : NetworkSystem
    {
        [SerializeField]
        private Mind _mindPrefab;

        [SyncObject]
        private readonly SyncList<Mind> _spawnedMinds = new();

        /// <summary>
        /// Tried to get the player's mind.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="mind"></param>
        /// <returns></returns>
        public bool TryGetMind(NetworkConnection player, out Mind mind)
        {
            PlayerControlSystem playerControlSystem = SystemLocator.Get<PlayerControlSystem>();
            Soul soul = playerControlSystem.GetSoul(player);

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
        /// <param name="player"></param>
        /// <returns></returns>
        public bool HasMind(NetworkConnection player)
        {
            Mind mind = _spawnedMinds.Find(mind => mind.Owner == player);

            return mind != null;
        }

        [Server]
        public bool TryCreateMind(NetworkConnection player, out Mind createdMind)
        {
            if (HasMind(player))
            {
                createdMind = null;
                return false;
            }

            Mind mind = Instantiate(_mindPrefab);
            ServerManager.Spawn(mind.GameObjectCache, player);

            createdMind = mind;
            return true;
        }
    }
}