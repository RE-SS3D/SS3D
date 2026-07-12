using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Shared toggle, pipe resolution, animator sync, and port registration for vents and scrubbers.
    /// </summary>
    [RequireComponent(typeof(PlacedTileObject))]
    public abstract class AtmosPortControllerBase : InteractionTargetNetworkBehaviour, IAtmosPortDevice, IToggleable
    {
        private static readonly int DeviceActiveId = Animator.StringToHash("deviceActive");

        [SerializeField]
        protected Animator _animator;

        [SyncVar(OnChange = nameof(HandleEnabledChanged))]
        private bool _enabled = true;

        [SyncVar(OnChange = nameof(HandleAnimatorActiveChanged))]
        private bool _animatorActive;

        private PlacedTileObject _tileObject;
        private GasPipeNetworkId _networkId = GasPipeNetworkId.None;

        public TileCoord OriginTile =>
            _tileObject != null
                ? new TileCoord(_tileObject.MapId, _tileObject.WorldOrigin)
                : default;

        public bool IsEnabled => _enabled;

        public bool TryGetConnectedNetwork(out GasPipeNetworkId networkId)
        {
            networkId = _networkId;
            return !networkId.IsNone;
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new ToggleInteraction
                {
                    OnName = "Turn off",
                    OffName = "Turn on",
                },
            };
        }

        public bool GetState() => _enabled;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Initialize();
            SubSystems.Get<AtmosSubSystem>()?.RegisterPort(this);
            RefreshAnimatorAuthorityState();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Initialize();
            ApplyAnimatorState(_animatorActive);
        }

        protected override void OnDestroyed()
        {
            if (SubSystems.TryGet(out AtmosSubSystem atmosSubSystem))
                atmosSubSystem.UnregisterPort(this);

            base.OnDestroyed();
        }

        [Server]
        public void Toggle()
        {
            _enabled = !_enabled;
            RefreshAnimatorAuthorityState();
        }

        public void ServerTick(AtmosPipeSimulation pipeSimulation, AtmosSimulation turfSimulation, float deltaTime)
        {
            ResolveConnectedNetwork();
            if (!_enabled || _networkId.IsNone)
            {
                SetAnimatorActive(false);
                return;
            }

            bool flowed = RunPortTick(pipeSimulation, turfSimulation, deltaTime);
            SetAnimatorActive(flowed || ShouldAnimateWhileIdle());
        }

        protected abstract bool RunPortTick(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            float deltaTime);

        protected virtual bool ShouldAnimateWhileIdle() => true;

        protected void ResolveConnectedNetwork()
        {
            _networkId = GasPipeNetworkId.None;
            if (_tileObject == null
                || !SubSystems.TryGet(out TileSubSystem tileSubSystem)
                || !SubSystems.TryGet(out AtmosSubSystem atmosSubSystem)
                || tileSubSystem.CurrentMap == null
                || atmosSubSystem.PipeRegistry == null)
            {
                return;
            }

            AtmosDevicePipeResolver.TryResolveNetwork(
                tileSubSystem.CurrentMap,
                atmosSubSystem.PipeRegistry,
                OriginTile,
                out _networkId,
                out _);
        }

        private void Initialize()
        {
            if (_tileObject == null)
                TryGetComponent(out _tileObject);

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
        }

        private void RefreshAnimatorAuthorityState()
        {
            if (!IsServer)
                return;

            SetAnimatorActive(_enabled && !_networkId.IsNone);
        }

        private void SetAnimatorActive(bool active)
        {
            if (_animatorActive == active)
                return;

            _animatorActive = active;
        }

        private void HandleEnabledChanged(bool _, bool __, bool asServer)
        {
            if (!asServer)
                RefreshAnimatorAuthorityState();
        }

        private void HandleAnimatorActiveChanged(bool _, bool newValue, bool asServer)
        {
            if (!asServer)
                ApplyAnimatorState(newValue);
        }

        private void ApplyAnimatorState(bool active)
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            if (_animator == null)
                return;

            _animator.SetBool(DeviceActiveId, active);
            ApplyDeviceSpecificAnimatorState(active);
        }

        protected virtual void ApplyDeviceSpecificAnimatorState(bool active)
        {
        }
    }
}
