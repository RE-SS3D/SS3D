using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Audio;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// The body's EVA switches (Documents/design/armor.md §3, §6). Internals need a tank plus a mask
    /// or sealed helmet, and the server turns them off the moment either goes away. Magboots need
    /// both feet, and hold only while the body is upright over a floor. Switches stay where
    /// they are once the body can no longer work them.
    /// </summary>
    public class EvaGearController : NetworkActor
    {
        /// <summary>Where a tank can feed internals from, best first. Bags do not count.</summary>
        private const float ClunkVolume = 0.7f;
        private const float ClunkPitchOn = 0.7f;
        private const float ClunkPitchOff = 0.9f;
        private const float ClunkRange = 10f;

        private static readonly ContainerType[] TankSlots =
        {
            ContainerType.SuitStorage,
            ContainerType.Pocket,
            ContainerType.Hand,
        };

        [SyncVar]
        private bool _internalsAvailable;

        [SyncVar]
        private bool _internalsOn;

        [SyncVar]
        private bool _magbootsAvailable;

        [SyncVar]
        private bool _magbootsOn;

        private HumanInventory _inventory;
        private Ragdoll _ragdoll;
        private HumanoidBodyStateMachine _bodyState;
        private HumanHealthController _health;

        /// <summary>Whether this body can work its switches. <see cref="HealthSnapshot.IsConscious"/> stays true in critical, so it cannot answer this.</summary>
        public bool CanSwitch => _health != null && _health.Snapshot.CanUseArms;

        public bool InternalsAvailable => _internalsAvailable;

        public bool InternalsOn => _internalsOn;

        public bool MagbootsAvailable => _magbootsAvailable;

        public bool MagbootsOn => _magbootsOn;

        /// <summary>Whether the boots hold right now. Built from synced state, so it reads the same on every peer.</summary>
        public bool MagbootsAnchored => _magbootsOn && !_ragdoll.IsKnockedDown && !_bodyState.Snapshot.IsFloating;

        /// <summary>World speed and clip playback scale while the boots hold.</summary>
        public float MovementSpeedFactor => MagbootsAnchored ? HealthConstants.MagbootSpeedFactor : 1f;

        protected override void OnAwake()
        {
            base.OnAwake();
            _ragdoll = GetComponent<Ragdoll>();
            _bodyState = GetComponent<HumanoidBodyStateMachine>();
            _health = GetComponent<HumanHealthController>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _inventory = GetComponent<HumanInventory>();
            _inventory.OnContainerContentChanged += HandleContainerContentChanged;
            ServerRefresh();
        }

        protected override void OnDestroyed()
        {
            if (_inventory != null)
            {
                _inventory.OnContainerContentChanged -= HandleContainerContentChanged;
            }

            base.OnDestroyed();
        }

        public void ClientToggleInternals() => CmdToggleInternals();

        [ServerRpc]
        private void CmdToggleInternals()
        {
            if (CanSwitch)
            {
                ServerSetInternals(!_internalsOn);
            }
        }

        [Server]
        public void ServerSetInternals(bool on)
        {
            ServerRefresh();
            _internalsOn = on && _internalsAvailable;
        }

        public void ClientToggleMagboots() => CmdToggleMagboots();

        [ServerRpc]
        private void CmdToggleMagboots()
        {
            if (CanSwitch)
            {
                ServerSetMagboots(!_magbootsOn);
            }
        }

        [Server]
        public void ServerSetMagboots(bool on)
        {
            ServerRefresh();
            bool next = on && _magbootsAvailable;
            if (next == _magbootsOn)
            {
                return;
            }

            _magbootsOn = next;
            SubSystems.Get<AudioSubSystem>().PlayAudioSource(
                AudioType.Sfx, AudioTrackIds.MagbootClunk, Transform.position, NetworkObject, false, ClunkVolume, next ? ClunkPitchOn : ClunkPitchOff, 1f, ClunkRange);
        }

        /// <summary>Whether wind should leave this body alone. A lost foot takes its boot without a content change.</summary>
        [Server]
        public bool ServerAnchored()
        {
            ServerRefresh();
            return MagbootsAnchored;
        }

        /// <summary>The tank being breathed from, or null while internals are off.</summary>
        [Server]
        public GasTankItemExtension ServerBreathingTank()
        {
            // A severed hand removes its container without a content change, so check every breath.
            ServerRefresh();
            return _internalsOn ? FindTank() : null;
        }

        private void HandleContainerContentChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            ServerRefresh();
        }

        [Server]
        private void ServerRefresh()
        {
            _internalsAvailable = FindTank() != null && HasBreathingGear();
            _internalsOn = _internalsOn && _internalsAvailable;
            _magbootsAvailable = Wears<MagbootItemExtension>(ContainerType.ShoeLeft) && _inventory.TryGetTypeContainer(ContainerType.ShoeRight, 0, out _);
            _magbootsOn = _magbootsOn && _magbootsAvailable;
        }

        private GasTankItemExtension FindTank()
        {
            foreach (ContainerType slot in TankSlots)
            {
                for (int i = 0; _inventory.TryGetTypeContainer(slot, i, out AttachedContainer container); i++)
                {
                    foreach (Item item in container.Items)
                    {
                        if (item.TryGetComponent(out GasTankItemExtension tank))
                        {
                            return tank;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>A sealed helmet counts as a mask even when breached (armor.md §3).</summary>
        private bool HasBreathingGear()
        {
            return Wears<BreathMaskItemExtension>(ContainerType.Mask) || Wears<SealedGearItemExtension>(ContainerType.Head);
        }

        private bool Wears<T>(ContainerType slot)
        {
            if (!_inventory.TryGetTypeContainer(slot, 0, out AttachedContainer container))
            {
                return false;
            }

            foreach (Item item in container.Items)
            {
                if (item.TryGetComponent(out T _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
