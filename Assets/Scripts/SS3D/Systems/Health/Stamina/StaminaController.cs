using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Manages the networking and out-of-process dependencies of the stamina subsystem
    /// </summary>
    public class StaminaController : NetworkActor
    {
        /// <summary>
        /// The stamina bar UI. It should only be modified on the client.
        /// </summary>
        private StaminaBarView _staminaBarView;

        /// <summary>
        /// The controller for this entity.
        /// </summary>
        [SerializeField] private HumanoidController _player;

        /// <summary>
        /// The PlayerControllable component for this entity.
        /// </summary>
        [SerializeField] private Entity _entity;

        /// <summary>
        /// Provides a way for the client to access the current player stamina.
        /// </summary>
        [SyncVar(OnChange = nameof(SyncCurrentStamina))] private float _currentStamina;

        /// <summary>
        /// Actual stamina data. Will only exist on the server.
        /// </summary>
        private IStamina _stamina;

        /// <summary>
        /// The current stamina proportion of the entity (scaled between zero and one).
        /// </summary>
        public float CurrentStamina => _currentStamina;

        public bool CanCommenceInteraction => IsServerOnly ? _stamina.CanCommenceInteraction : _currentStamina > 0f;

        /// <summary>
        /// TODO: Refactor how this works.
        /// This is very much a hack while we are waiting for movement to be server authoritative.
        /// Note that the server and client responses are not equivalent. The server will let you
        /// continue an interaction while slightly in negative stamina, whereas the client will
        /// require that you stop as soon as you hit zero stamina. This problem will go away once
        /// movement is server authoritative because this will only be needed on the server.
        /// </summary>
        public bool CanContinueInteraction => IsServerOnly ? _stamina.CanContinueInteraction : _currentStamina > 0f;

        protected override void OnStart()
        {
            base.OnStart();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            // The server manages the stamina data for each entity.
            _stamina = StaminaFactory.Create();
            _currentStamina = _stamina.Current;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _staminaBarView = ViewLocator.Get<StaminaBarView>().First();
            // Currently movement is client-authoritative, so we need to subscribe to events on the client only.
            SubscribeToEvents();
            InitialAssignViewToControllable();
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (IsServer)
            {
                _stamina.RechargeStamina(updateEvent.DeltaTime);
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _player.OnSpeedChangeEvent += DepleteStamina;
            _entity.OnMindChanged += AssignViewToControllable;
        }

        private void UnsubscribeFromEvents()
        {
            _player.OnSpeedChangeEvent -= DepleteStamina;
            _entity.OnMindChanged -= AssignViewToControllable;

        }

        [Client]
        private void AssignViewToControllable(Mind mind)
        {
            if (mind == null || !mind.IsOwner)
            {
                _staminaBarView.UnassignViewFromPlayer(this);
            }
            else
            {
                _staminaBarView.AssignViewToPlayer(this);
            }
        }

        /// <summary>
        /// Depletes stamina by a set amount. To be called only from server-side scripts (e.g. Interactions)
        /// </summary>
        /// /// <param name="amountToDeplete">The amount of stamina to reduce</param>
        [Server]
        public void ServerDepleteStamina(float amountToDeplete)
        {
            _stamina.ConsumeStamina(amountToDeplete);
            _currentStamina = _stamina.Current;
        }

        /// <summary>
        /// Where relevant, assigns the Stamina Bar View to this entity.
        /// Required because Mind is changed / set before the OnMindSet event is subscribed to.
        /// </summary>
        private void InitialAssignViewToControllable()
        {
            if (_entity.Mind != null && _entity.Mind.IsOwner)
            {
                AssignViewToControllable(_entity.Mind);
            }
        }

        /// <summary>
        /// This method simply takes a value to reduce by, scales it to time and passes it to the server via RPC.
        /// To be called only from client-side scripts (e.g. Movement)
        /// </summary>
        /// <param name="rawAmountToDeplete">The amount of stamina to reduce per second (not yet scaled to delta time)</param>
        private void DepleteStamina(float rawAmountToDeplete)
        {
            if (IsOwner)
            {
                CmdDepleteStaminaScaled(rawAmountToDeplete * Time.deltaTime);
            }
        }

        /// <summary>
        /// This method takes a value to reduce stamina by. All time scaling has already been completed.
        /// </summary>
        /// <param name="amountToDeplete">The amount of stamina to reduce</param>
        [ServerRpc]
        private void CmdDepleteStaminaScaled(float amountToDeplete)
        {
            _stamina.ConsumeStamina(amountToDeplete);
            _currentStamina = _stamina.Current;
        }

        private void SyncCurrentStamina(float old, float value, bool asServer)
        {

        }
    }
}