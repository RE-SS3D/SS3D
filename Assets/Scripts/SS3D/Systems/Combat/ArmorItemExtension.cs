using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Per-zone absorption + integrity for a worn armor piece (Documents/design/armor.md §2).
    /// Sits alongside Item on the armor prefab, same discovery pattern as RangedWeaponItemExtension
    /// (item.TryGetComponent&lt;ArmorItemExtension&gt;()). Integrity has no client-prediction need,
    /// so unlike RangedWeaponItemExtension it's a plain SyncVar instead of manual RPC mirroring.
    /// </summary>
    public class ArmorItemExtension : NetworkBehaviour
    {
        [SerializeField] private ArmorProfile _profile = ArmorProfile.SecurityJumpsuit;

        [SyncVar]
        private float _integrity;

        public ArmorProfile Profile => _profile;

        public float Integrity => _integrity;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _integrity = _profile.MaxIntegrity;
        }

        /// <summary>
        /// Absorbs one hit against this piece, depleting integrity by the amount actually absorbed.
        /// Returns the damage that gets through to the limb.
        /// </summary>
        [Server]
        public (float RemainingBrute, float RemainingBurn) ServerAbsorb(float incomingBrute, float incomingBurn)
        {
            (float remainingBrute, float remainingBurn, float integrityLoss) =
                ArmorSimulation.ResolveAbsorption(_profile, _integrity, incomingBrute, incomingBurn);

            _integrity -= integrityLoss;
            return (remainingBrute, remainingBurn);
        }
    }
}
