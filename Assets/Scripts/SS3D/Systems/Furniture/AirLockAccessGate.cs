using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Resolves door access requirements from a per-door override or the destination area default.
    /// </summary>
    [RequireComponent(typeof(AuthLogDeviceBehaviour))]
    public sealed class AirLockAccessGate : MonoBehaviour
    {
        [SerializeField]
        private bool _requireAccess = true;

        // Old prefab masks described the door skin (civilian/security), not an authored exception.
        [FormerlySerializedAs("_overrideRequiredAccessBits")]
        [SerializeField]
        private ulong _fallbackRequiredAccessBits;

        private AuthLogDeviceBehaviour _authLogDevice;

        public IAuthLogDevice AuthLogDevice => _authLogDevice;

        private void Awake()
        {
            _authLogDevice = GetComponent<AuthLogDeviceBehaviour>();
        }

        public AccessMask ResolveRequiredAccess()
        {
            if (!_requireAccess)
            {
                return AccessMask.None;
            }

            TryGetAreaRecord(out AreaRecord record);
            return ResolveAreaRequiredAccess(record, new AccessMask(_fallbackRequiredAccessBits));
        }

        /// <summary>Assigned room metadata overrides legacy door-skin defaults, including public rooms.</summary>
        public static AccessMask ResolveAreaRequiredAccess(AreaRecord record, AccessMask fallback)
        {
            if (record == null)
                return fallback;

            return !record.DefaultRequiredAccess.IsNone
                ? record.DefaultRequiredAccess
                : AreaAccessDefaults.FromParentTag(record.ParentTag);
        }

        public bool TryAuthorizeCollider(Collider other, out HumanInventory inventory, out AccessCheckResult result)
        {
            inventory = other != null ? other.GetComponentInParent<HumanInventory>() : null;
            return TryAuthorizeInventory(inventory, out result);
        }

        public bool TryAuthorizeInventory(HumanInventory inventory, out AccessCheckResult result)
        {
            result = AccessCheckResult.Fail(AccessCheckFailureReason.NoCredential);

            if (!_requireAccess)
            {
                result = AccessCheckResult.Pass(new CrewRecordId(CrewRecordId.None), AccessMask.None);
                return true;
            }

            if (inventory == null)
            {
                return false;
            }

            AccessMask required = ResolveRequiredAccess();
            if (required.IsNone)
            {
                result = AccessCheckResult.Pass(new CrewRecordId(CrewRecordId.None), AccessMask.None);
                return true;
            }

            if (!SubSystems.TryGet(out IdAccessSubSystem idAccess))
            {
                return false;
            }

            result = idAccess.CheckAccess(inventory, required, _authLogDevice);
            return result.Passed;
        }

        private bool TryGetAreaRecord(out AreaRecord record)
        {
            record = null;

            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                || !SubSystems.TryGet(out TileSubSystem tileSubSystem))
            {
                return false;
            }

            TileCoord coord = tileSubSystem.QueryService.WorldToTile(transform.position);
            return areaSubSystem.TryGetAreaForTile(coord, out record);
        }
    }
}
