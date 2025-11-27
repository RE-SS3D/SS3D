using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public class FeetController : NetworkActor
    {
        public event Action<float> OnFeetHealthChanged;

        /// <summary>
        /// List of feet used by the player
        /// </summary>
        private List<FootBodyPart> _feet;

        /// <summary>
        /// Number of optimal feet to move at the maximum speed. For a human, more than two feets is useless, and less makes you run slower (or not at all).
        /// </summary>
        private int _optimalFeetNumber = 2;

        /// <summary>
        /// Factor influencing the speed of a player, based upon state of feets. Should be between 0 and 1 in value.
        /// </summary>
        [SyncVar]
        private float _feetHealthFactor;

        private float _minFeetHealthFactorToStand = 0.5f;

        public float FeetHealthFactor => _feetHealthFactor;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _feet = GetComponentsInChildren<FootBodyPart>().ToList();

            foreach (FootBodyPart foot in _feet)
            {
                foot.OnDamageInflicted += HandleFootHurt;
                foot.OnBodyPartDetached += HandleFootRemoved;
                foot.OnBodyPartDestroyed += HandleFootRemoved;
            }

            _feetHealthFactor = 1;
        }

        /// <summary>
        /// Update feet health factor when a foot takes damages.
        /// </summary>
        [Server]
        private void HandleFootHurt(object sender, EventArgs e)
        {
            UpdateFeetHealthFactor();
        }

        /// <summary>
        /// If a foot is destroyed or detached, remove it from the list of available feet.
        /// </summary>
        [Server]
        private void HandleFootRemoved(object sender, EventArgs e)
        {
            FootBodyPart foot = (FootBodyPart)sender;

            if (foot != null)
            {
                _feet.Remove(foot);
            }

            UpdateFeetHealthFactor();
        }

        /// <summary>
        /// Simply update the feet health factor, based on the health of each available feet and their numbers.
        /// </summary>
        [Server]
        private void UpdateFeetHealthFactor()
        {
            _feetHealthFactor = _feet.Sum(x => x.GetSpeedContribution()) / _optimalFeetNumber;
            OnFeetHealthChanged?.Invoke(_feetHealthFactor);
        }
    }
}
