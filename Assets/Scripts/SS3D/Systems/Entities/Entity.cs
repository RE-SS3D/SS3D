using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Base class for all things that can be controlled by a player.
    /// </summary>
    [Serializable]
    public class Entity : NetworkActor
    {
        public event Action<Character> OnCharacterChanged;

        [SerializeField]
        [SyncVar(OnChange = nameof(SyncCharacter))]
        private Character character = Character.Empty;

        public Character Character
        {
            get => character;
            set => character = value;
        }

        public string Ckey => character.player.Ckey;

        protected override void OnStart()
        {
            base.OnStart();

            OnSpawn();
        }

        private void OnSpawn()
        {
            OnCharacterChanged?.Invoke(Character);
        }

        private void InvokeLocalPlayerObjectChanged()
        {
            if (Character == null) return;

            if (!Character.player.IsLocalConnection)
            {
                return;
            }

            LocalPlayerObjectChanged localPlayerObjectChanged = new(GameObject);
            localPlayerObjectChanged.Invoke(this);
        }

        /// <summary>
        /// Called by FishNet when the value of _character is synced.
        /// </summary>
        /// <param name="oldCharacter">Value before sync</param>
        /// <param name="newCharacter">Value after sync</param>
        /// <param name="asServer">Is the sync is being called as the server (host and server only)</param>
        public void SyncCharacter(Character oldCharacter, Character newCharacter, bool asServer)
        {
            if (!asServer && IsHost)
            {
                return;
            }

            OnCharacterChanged?.Invoke(character);
            InvokeLocalPlayerObjectChanged();
        }

        /// <summary>
        /// Updates the character of this entity.
        /// </summary>
        /// <param name="character">The new character.</param>
        [Server]
        public void SetCharacter(Character character)
        {
            this.character = character;

            GiveOwnership(character.Owner);
        }
    }
}