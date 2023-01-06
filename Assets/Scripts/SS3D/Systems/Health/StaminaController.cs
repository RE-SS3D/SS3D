using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health.UI;
using System;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Manages the networking and out-of-process dependencies of the stamina subsystem
    /// </summary>
    [RequireComponent(typeof(HumanoidController))]
    public class StaminaController : NetworkActor
    {
        /// <summary>
        /// Event fired when the stamina is updated.
        /// </summary>
        public event Action<float> OnStaminaChanged;

        /// <summary>
        /// The stamina bar UI
        /// </summary>
        private StaminaBarView UI;

        /// <summary>
        /// The type of stamina data to create
        /// </summary>
        [SerializeField] private StaminaType _staminaType;

        /// <summary>
        /// The controller for this entity.
        /// </summary>
        [SerializeField] private HumanoidController _player;


        [SerializeField] private PlayerControllable playerControllable;

        /// <summary>
        /// Provides a way for the client to access the current player stamina.
        /// </summary>
        [SyncVar] private float _current;

        public float Current
        {
            get { return _current; }
        }

        /// <summary>
        /// Actual stamina data. Will only exist on the server.
        /// </summary>
        private IStamina _stamina;

        protected override void OnStart()
        {
            base.OnStart();

            // The server manages the stamina data for each entity.
            if (IsServer)
            {
                _stamina = StaminaHelper.Create(_staminaType, 10f);
                _current = _stamina.Current;
            }

            // Currently movement is client-authoritative, so we need to subscribe to events on the client only.
            if (IsClient && playerControllable.ControllingSoul.IsOwner)
            {
                SubscribeToEvents();
                UI = FindObjectOfType<StaminaBarView>();
                UI?.AssignViewToPlayer(this);
            }

            
        }

        public bool CanCommenceInteraction
        {
            get => IsServerOnly ? _stamina.CanCommenceInteraction : _current > 0f;
        }

        public bool CanContinueInteraction
        {
            // This is very much a hack while we are waiting for movement to be server authoritative.
            // Note that the server and client responses are not equivalent. The server will let you
            // continue an interaction while slightly in negative stamina, whereas the client will
            // require that you stop as soon as you hit zero stamina. This problem will go away once
            // movement is server authoritative because this will only be needed on the server.
            get => IsServerOnly ? _stamina.CanContinueInteraction : _current > 0f;
        }

        public float CurrentStamina
        {
            get => _current;
        }

        /// <summary>
        /// This method simply takes a value to reduce by, scales it to time and passes it to the server via RPC.
        /// </summary>
        /// <param name="rawAmountToDeplete">The amount of stamina to reduce per second (not yet scaled to delta time)</param>
        private void DepleteStamina(float rawAmountToDeplete)
        {
            DepleteStaminaScaled(rawAmountToDeplete * Time.deltaTime);
        }

        /// <summary>
        /// This method takes a value to reduce stamina by. All time scaling has already been completed.
        /// </summary>
        /// <param name="amountToDeplete">The amount of stamina to reduce</param>
        [ServerRpc]
        private void DepleteStaminaScaled(float amountToDeplete)
        {
            _stamina.ConsumeStamina(amountToDeplete);
            _current = _stamina.Current;
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            if (IsServer)
            {
                _stamina.RechargeStamina(deltaTime);
            }

            if (!IsOwner)
            {
                return;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _player.OnSpeedChanged += DepleteStamina;
        }

        private void UnsubscribeFromEvents()
        {
            _player.OnSpeedChanged -= DepleteStamina;
        }
    }
}