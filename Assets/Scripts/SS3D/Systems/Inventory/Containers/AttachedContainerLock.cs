using SS3D.Core;
using SS3D.Systems.IdAccess;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Minimal ID-gated lock for freestanding world containers (lockers, crates) — reuses
    /// IdAccessSubSystem.CheckAccess the same way every other consumer does
    /// (Documents/architecture/systems/id-access.md "New consumer"). Personal worn containers
    /// (backpack, pockets, belt) never get this component — see design doc §8.
    ///
    /// Deliberately does not expose the hacker-visible raw access bitmask from
    /// hacking-interface.md §3's FDU taxonomy; that integration is deferred until
    /// hacking-interface has its own architecture effort. See
    /// Documents/architecture/2026-07_inventory-storage-redesign.md "Scope decisions".
    /// </summary>
    [RequireComponent(typeof(AttachedContainer))]
    [RequireComponent(typeof(AuthLogDeviceBehaviour))]
    public sealed class AttachedContainerLock : MonoBehaviour
    {
        [Tooltip("Access bits required to open/store/take from this container. None means unlocked.")]
        [SerializeField]
        private ulong _requiredAccessBits;

        private AuthLogDeviceBehaviour _authLogDevice;

        public AccessMask RequiredAccess => new(_requiredAccessBits);

        public bool IsLocked => !RequiredAccess.IsNone;

        private void Awake()
        {
            _authLogDevice = GetComponent<AuthLogDeviceBehaviour>();
        }

        /// <summary>
        /// Server-authoritative access check for opening/storing/taking from this container.
        /// Crew without access simply see the container as locked — no hacker bitmask view this pass.
        /// </summary>
        public bool IsAccessGranted(HumanInventory inventory)
        {
            if (!IsLocked)
            {
                return true;
            }

            if (inventory == null || !SubSystems.TryGet(out IdAccessSubSystem idAccess))
            {
                return false;
            }

            return idAccess.CheckAccess(inventory, RequiredAccess, _authLogDevice).Passed;
        }
    }
}
