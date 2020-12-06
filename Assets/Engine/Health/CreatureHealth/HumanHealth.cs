using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Content.Creatures.Human;
using UnityEngine;

namespace SS3D.Engine.Health
{
    /// <summary>
    /// Attach to humans which feature a metabolic system.
    /// </summary>
    public class HumanHealth : CreatureHealth
    {
        [SerializeField]
        private MetabolismSystem metabolism;
        public MetabolismSystem Metabolism { get => metabolism; }

        private bool init = false;

        void Start()
        {
            EnsureInit();
        }

        void EnsureInit()
        {
            if (init)
                return;

            init = true;
            OnConsciousStateChangeServer.AddListener(OnPlayerConsciousStateChangeServer);

            metabolism = GetComponent<MetabolismSystem>();
            if (metabolism == null)
            {
                metabolism = gameObject.AddComponent<MetabolismSystem>();
            }
        }

        public override void OnStartClient()
        {
            EnsureInit();
            base.OnStartClient();
        }

        public override void OnStartServer()
        {
            EnsureInit();
            base.OnStartServer();
        }

        /// <summary>
        /// Handles actions after death (e.g. dropping inventory).
        /// </summary>
        protected override void OnDeathActions()
        {
            HumanRagdoll ragdoll = GetComponent<HumanRagdoll>();
            ragdoll.KnockDown(100); // We will just knockdown the player for 100 seconds now

            Debug.Log("A player died");
        }

        [Server]
        public void ServerGibPlayer()
        {
            Gib();
        }

        protected override void Gib()
        {
            // TODO: Implement proper gib logic
            Death();
        }

        // Make player unconscious upon crit
        private void OnPlayerConsciousStateChangeServer(ConsciousState oldState, ConsciousState newState)
        {
            // TODO: Make player unconscious
        }
    }
}