using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Tile;
using UnityEngine;

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

        [SerializeField]
        private bool _useAreaDefault = true;

        [SerializeField]
        private ulong _overrideRequiredAccessBits;

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

            if (_overrideRequiredAccessBits != 0)
            {
                return new AccessMask(_overrideRequiredAccessBits);
            }

            if (_useAreaDefault && TryGetAreaRecord(out AreaRecord record))
            {
                if (!record.DefaultRequiredAccess.IsNone)
                {
                    return record.DefaultRequiredAccess;
                }

                AccessMask fromTag = AreaAccessDefaults.FromParentTag(record.ParentTag);
                if (!fromTag.IsNone)
                {
                    return fromTag;
                }
            }

            return AccessMask.None;
        }

        public bool TryAuthorizeCollider(Collider other, out HumanInventory inventory, out AccessCheckResult result)
        {
            inventory = null;
            result = AccessCheckResult.Fail(AccessCheckFailureReason.NoCredential);

            if (!_requireAccess)
            {
                result = AccessCheckResult.Pass(new CrewRecordId(CrewRecordId.None), AccessMask.None);
                return true;
            }

            inventory = other.GetComponentInParent<HumanInventory>();
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
