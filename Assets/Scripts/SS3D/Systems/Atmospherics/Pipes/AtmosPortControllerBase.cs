using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Audio;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using SS3D.Systems.Electricity;
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
        private int _resolvedTopologyVersion = -1;

        public TileCoord OriginTile =>
            _tileObject != null
                ? new TileCoord(_tileObject.MapId, _tileObject.WorldOrigin)
                : default;

        public bool IsEnabled => _enabled;

        public bool IsPortFlowing => _portFlowing;

        public bool IsLoopRunning => _portFlowing;

        public virtual bool TryGetConnectedNetwork(out GasPipeNetworkId networkId)
        {
            networkId = _networkId;
            return !networkId.IsNone;
        }

        /// <summary>
        /// The placed object behind this device.
        /// </summary>
        protected PlacedTileObject TileObject
        {
            get
            {
                // Map load refreshes pipe adjacency before devices start.
                if (_tileObject == null)
                    TryGetComponent(out _tileObject);

                return _tileObject;
            }
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return System.Array.Empty<IInteraction>();
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
            ApplyClientVisualState();
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

        [Server]
        public void ServerSetEnabled(bool enabled)
        {
            if (_enabled != enabled)
                _enabled = enabled;

            RefreshAnimatorAuthorityState();
        }

        public void ServerTick(AtmosPipeSimulation pipeSimulation, AtmosSimulation turfSimulation, float deltaTime)
        {
            ResolveConnectedNetwork();

            if (!IsPowered())
            {
                SetAnimatorActive(false, force: true);
                SetPortFlowing(false, force: true);
                return;
            }

            if (!_enabled)
            {
                SetAnimatorActive(false, force: true);
                SetPortFlowing(false, force: true);
                return;
            }

            if (!TryGetConnectedNetwork(out _))
            {
                SetAnimatorActive(ShouldAnimateWhileIdle());
                SetPortFlowing(false);
                return;
            }

            bool flowed = RunPortTick(pipeSimulation, turfSimulation, deltaTime);
            SetAnimatorActive(true);
            SetPortFlowing(flowed);
        }

        /// <summary>
        /// The pipe lines this device attaches to, in order, on its own tile.
        /// </summary>
        public abstract TileLayer[] PipeLayers { get; }

        protected abstract bool RunPortTick(
            AtmosPipeSimulation pipeSimulation,
            AtmosSimulation turfSimulation,
            float deltaTime);

        protected virtual bool ShouldAnimateWhileIdle() => true;

        protected virtual void ResolveConnectedNetwork()
        {
            if (!TryGetPipeContext(out TileMap map, out GasPipeNetworkRegistry registry))
                return;

            int topologyVersion = registry.TopologyVersion;
            if (!_networkId.IsNone
                && _resolvedTopologyVersion == topologyVersion
                && registry.TryGetNetwork(_networkId, out _))
            {
                return;
            }

            _networkId = GasPipeNetworkId.None;
            AtmosDevicePipeResolver.TryResolveNetwork(
                map,
                registry,
                new TileCoord(_tileObject.MapId, _tileObject.WorldOrigin),
                PipeLayers,
                out _networkId,
                out _);
            _resolvedTopologyVersion = topologyVersion;
        }

        /// <summary>
        /// Releases up to <paramref name="budgetMoles"/> of a network's mix onto this device's tile,
        /// each gas by its share.
        /// </summary>
        protected bool ReleaseToTurf(
            AtmosPipeSimulation pipeSimulation,
            GasPipeNetworkId networkId,
            GasPipeNetworkRecord network,
            float budgetMoles)
        {
            float totalMoles = network.GetTotalMoles(AtmosConstants.DefaultGasCount);
            if (budgetMoles <= 0f || totalMoles <= 0f)
                return false;

            budgetMoles = Mathf.Min(budgetMoles, totalMoles);
            bool movedAny = false;

            for (int gasId = 0; gasId < AtmosConstants.DefaultGasCount; gasId++)
            {
                float gasMoles = network.Moles[gasId];
                if (gasMoles <= 0f)
                    continue;

                if (pipeSimulation.TryTransferMoles(
                        networkId,
                        new GasId((ushort)gasId),
                        budgetMoles * (gasMoles / totalMoles),
                        OriginTile,
                        PipeTransferDirection.ToTurf,
                        out float moved)
                    && moved > 0f)
                {
                    movedAny = true;
                }
            }

            return movedAny;
        }

        protected bool TryGetPipeContext(out TileMap map, out GasPipeNetworkRegistry registry)
        {
            map = null;
            registry = null;
            if (_tileObject == null
                || !SubSystems.TryGet(out TileSubSystem tileSubSystem)
                || !SubSystems.TryGet(out AtmosSubSystem atmosSubSystem))
            {
                return false;
            }

            map = tileSubSystem.CurrentMap;
            registry = atmosSubSystem.PipeRegistry;
            return map != null && registry != null;
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
                SetAnimatorActive(false, force: true);
                SetPortFlowing(false, force: true);
                return;
            }

            ResolveConnectedNetwork();
            bool connected = TryGetConnectedNetwork(out _);
            SetAnimatorActive(connected || ShouldAnimateWhileIdle());
            if (!connected)
                SetPortFlowing(false, force: true);
        }

        protected bool IsPowered() => AtmosPortPower.IsPowered(_powerConsumer);

        private void SetAnimatorActive(bool active, bool force = false)
        {
            if (!force && _animatorActive == active)
                return;

            _animatorActive = active;

            // In edit-mode tests and some editor-only contexts, FishNet may not have a NetworkObject yet.
            if (NetworkObject != null && IsClient)
                ApplyAnimatorState(active);
        }

        private void SetPortFlowing(bool flowing, bool force = false)
        {
            if (!force && _portFlowing == flowing)
                return;

            _portFlowing = flowing;

            if (NetworkObject != null && IsClient)
                ApplyPortFlowingState(flowing);
        }

        private void HandleEnabledChanged(bool _, bool __, bool asServer)
        {
            if (!asServer)
                ApplyClientVisualState();
        }

        private void HandleAnimatorActiveChanged(bool _, bool __, bool asServer)
        {
            if (!asServer)
                ApplyClientVisualState();
        }

        private void HandlePortFlowingChanged(bool _, bool __, bool asServer)
        {
            if (!asServer)
                ApplyClientVisualState();
        }

        private void ApplyClientVisualState()
        {
            if (NetworkObject == null || !IsClient)
                return;

            ApplyEnabledVisual(_enabled);
            bool deviceActive = _enabled && IsPowered() && _animatorActive;
            bool flowing = _enabled && IsPowered() && _portFlowing;
            ApplyAnimatorState(deviceActive);
            ApplyPortFlowingState(flowing);
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
            if (!deviceActive)
                ApplyDeviceSpecificAnimatorState(false);
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

        protected virtual void ApplyEnabledVisual(bool enabled)
        {
        }

        protected virtual void ApplyDeviceSpecificAnimatorState(bool flowing)
        {
        }
    }
}
