using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using System.Electricity;
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

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        [SyncVar(OnChange = nameof(HandleEnabledChanged))]
        private bool _enabled = true;

        [SyncVar(OnChange = nameof(HandleAnimatorActiveChanged))]
        private bool _animatorActive;

        [SyncVar(OnChange = nameof(HandlePortFlowingChanged))]
        private bool _portFlowing;

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
                    CanInteractCallback = _ => IsPowered(),
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
            ApplyPortFlowingState(_portFlowing);
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

            if (!IsPowered())
            {
                SetAnimatorActive(false);
                SetPortFlowing(false);
                return;
            }

            if (!_enabled)
            {
                SetAnimatorActive(false);
                SetPortFlowing(false);
                return;
            }

            if (_networkId.IsNone)
            {
                SetAnimatorActive(ShouldAnimateWhileIdle());
                SetPortFlowing(false);
                return;
            }

            bool flowed = RunPortTick(pipeSimulation, turfSimulation, deltaTime);
            SetAnimatorActive(true);
            SetPortFlowing(flowed);
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
                _animator = GetComponent<Animator>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
        }

#if UNITY_EDITOR
        [SerializeField]
        private bool _previewAnimatorInEditor = true;

        private void OnEnable()
        {
            if (UnityEngine.Application.isPlaying || !_previewAnimatorInEditor)
                return;

            Initialize();
            ApplyAnimatorState(true);
            ApplyPortFlowingState(false);
        }

        private void Update()
        {
            if (UnityEngine.Application.isPlaying || !_previewAnimatorInEditor || _animator == null)
                return;

            _animator.Update(Time.deltaTime);
        }
#endif

        private void RefreshAnimatorAuthorityState()
        {
            if (!IsServer)
                return;

            if (!IsPowered() || !_enabled)
            {
                SetAnimatorActive(false);
                SetPortFlowing(false);
                return;
            }

            SetAnimatorActive(!_networkId.IsNone || ShouldAnimateWhileIdle());
            if (_networkId.IsNone)
                SetPortFlowing(false);
        }

        protected bool IsPowered() => AtmosPortPower.IsPowered(_powerConsumer);

        private void SetAnimatorActive(bool active)
        {
            if (_animatorActive == active)
                return;

            _animatorActive = active;

            if (IsClient)
                ApplyAnimatorState(active);
        }

        private void SetPortFlowing(bool flowing)
        {
            if (_portFlowing == flowing)
                return;

            _portFlowing = flowing;

            if (IsClient)
                ApplyPortFlowingState(flowing);
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

        private void HandlePortFlowingChanged(bool _, bool newValue, bool asServer)
        {
            if (!asServer)
                ApplyPortFlowingState(newValue);
        }

        private void ApplyAnimatorState(bool deviceActive)
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                    _animator = GetComponentInChildren<Animator>(true);
            }

            if (_animator == null)
                return;

            _animator.SetBool(DeviceActiveId, deviceActive);
        }

        private void ApplyPortFlowingState(bool flowing)
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                    _animator = GetComponentInChildren<Animator>(true);
            }

            if (_animator == null)
                return;

            ApplyDeviceSpecificAnimatorState(flowing);
        }

        protected virtual void ApplyDeviceSpecificAnimatorState(bool flowing)
        {
        }
    }
}
