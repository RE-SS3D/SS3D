using System.Collections;
using System.Collections.Generic;
using Mirror;
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