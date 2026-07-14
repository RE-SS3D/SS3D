using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Linq;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;

namespace SS3D.Systems.Stamina
{
    /// <summary>
    /// Manages the networking and out-of-process dependencies of the stamina subsystem.
    /// </summary>
    public class StaminaController : NetworkActor
    {
        private StaminaBarView _staminaBarView;

        [SerializeField] private HumanoidController _player;
        [SerializeField] private Entity _entity;

        [SyncVar(OnChange = nameof(SyncCurrentStamina))] private float _currentStamina;

        private IStamina _stamina;

        public float CurrentStamina => _currentStamina;

        public bool CanCommenceInteraction => IsServerOnly ? _stamina.CanCommenceInteraction : _currentStamina > 0f;

        /// <summary>
        /// TODO: Refactor when movement is server authoritative.
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
            _stamina = StaminaFactory.Create();
            _currentStamina = _stamina.Current;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _staminaBarView = ViewLocator.Get<StaminaBarView>().First();
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

        [Server]
        public void ServerDepleteStamina(float amountToDeplete)
        {
            _stamina.ConsumeStamina(amountToDeplete);
            _currentStamina = _stamina.Current;
        }

        private void InitialAssignViewToControllable()
        {
            if (_entity.Mind != null && _entity.Mind.IsOwner)
            {
                AssignViewToControllable(_entity.Mind);
            }
        }

        private void DepleteStamina(float rawAmountToDeplete)
        {
            if (IsOwner)
            {
                CmdDepleteStaminaScaled(rawAmountToDeplete * Time.deltaTime);
            }
        }

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
