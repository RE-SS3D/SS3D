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
    public class CharacterSystem : NetworkSystem
    {
        [SerializeField]
        private GameObject _characterPrefab;

        [SerializeField]
        private Character _emptyCharacter;

        [SyncObject]
        private readonly SyncList<Character> _spawnedCharacters = new();

        public Character EmptyCharacter => _emptyCharacter;

        /// <summary>
        /// Tried to get the player's character.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool TryGetCharacter(Player player, out Character character)
        {
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            // todo inspect do we really need to GetPlayer when we pass a player already?
            Player actualPlayer = playerSystem.GetPlayer(player.Owner);

            character = _spawnedCharacters.Find(spawnedCharacter => spawnedCharacter.player == actualPlayer);

            if (character != null)
            {
                return true;
            }

            character = null;
            return false;
        }

        /// <summary>
        /// Returns if a user has a character or not.
        /// </summary>
        /// <param name="player</param>
        /// <returns></returns>
        public bool HasCharacter(Player player)
        {
            Character character = _spawnedCharacters.Find(spawnedCharacter => spawnedCharacter.player == player);

            return character != null;
        }

        /// <summary>
        /// Tries to create a character for a user.
        /// </summary>
        /// <param name="player</param>
        /// <param name="createdCharacter"></param>
        /// <returns></returns>
        [Server]
        public bool TryCreateCharacter(Player player, out Character createdCharacter)
        {
            if (HasCharacter(player))
            {
                createdCharacter = null;
                return false;
            }

            Character character = Instantiate(_characterPrefab).GetComponent<Character>();
            ServerManager.Spawn(character.GameObject, player.Owner);

            character.SetPlayer(player);

            createdCharacter = character;
            return true;
        }

        /// <summary>
        /// Asks the server to execute a character swap.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        [ServerRpc]
        public void CmdSwapCharacters(Entity origin, Entity target, NetworkConnection networkConnection = null)
        {
            SwapCharacters(origin, target);
        }

        /// <summary>
        /// Executes a character swap.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        [Server]
        private void SwapCharacters(Entity origin, Entity target)
        {
            Character originCharacter = origin.Character;
            Character targetCharacter = target.Character;

            origin.SetCharacter(targetCharacter);
            target.SetCharacter(originCharacter);
        }
    }
}