using FishNet.Object;
using SS3D.Core;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// One-shot delivery beacon for the Syndicate bomb (the /tg/ uplink beacon, without an uplink yet). Used in
    /// hand, it is consumed and the bomb appears on the floor in front of the user, so only call it down when ready.
    /// </summary>
    public sealed class SyndicateBombBeacon : InteractionTargetNetworkBehaviour
    {
        /// <summary>How far in front of the user the bomb lands.</summary>
        public const float DropDistance = 0.9f;

        [SerializeField] private ObjectAssetReference _bombAsset;
        [SerializeField] private string _arrivalSoundId;

        private bool _used;

        public override IInteraction[] CreateTargetInteractions(InteractionEvent e) =>
            new IInteraction[] { new ActivateInteraction(this) };

        private Hand HeldHand(InteractionEvent e)
        {
            Hand hand = e?.Source?.GetRootSource() as Hand;
            return hand != null && hand.ItemInHand == GetComponent<Item>() ? hand : null;
        }

        /// <summary>Server: drops the bomb in front of <paramref name="user"/> and consumes the beacon.</summary>
        [Server]
        public Item ServerCallBomb(Transform user)
        {
            if (_used || user == null || _bombAsset == null || !SubSystems.TryGet(out ItemSubSystem items))
            {
                return null;
            }

            Vector3 forward = Vector3.ProjectOnPlane(user.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 drop = DropPoint(user, forward);
            _used = true;
            Item bomb = items.SpawnItemAtRest(_bombAsset.Id, drop, Quaternion.LookRotation(forward).eulerAngles.y);
            if (!string.IsNullOrEmpty(_arrivalSoundId) && SubSystems.TryGet(out AudioSubSystem audio))
            {
                audio.PlayAudioSource(AudioType.Sfx, _arrivalSoundId, drop, null, volume: 0.8f, minRange: 1f, maxRange: 15f);
            }

            GetComponent<Item>().Delete();
            return bomb;
        }

        /// <summary>
        /// In front of the user, pulled back to the user's own tile if a wall is in the way, a little above the
        /// floor so it settles rather than spawning inside it.
        /// </summary>
        private static Vector3 DropPoint(Transform user, Vector3 forward)
        {
            Vector3 feet = user.position;
            Vector3 origin = feet + Vector3.up * 0.5f;
            float distance = DropDistance;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, forward, DropDistance + 0.35f,
                LayerMask.GetMask("Default", "Walls"), QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(user))
                {
                    distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.35f));
                }
            }

            return feet + forward * distance + Vector3.up * 0.15f;
        }

        private sealed class ActivateInteraction : IInteraction
        {
            private readonly SyndicateBombBeacon _beacon;
            public ActivateInteraction(SyndicateBombBeacon beacon) => _beacon = beacon;
            public int Priority => 40;
            public string GetName(InteractionEvent e) => "Call down Syndicate bomb";
            public string GetGenericName() => "SyndicateBombBeacon";
            public Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Power;
            public bool CanInteract(InteractionEvent e) => _beacon != null && !_beacon._used && _beacon.HeldHand(e) != null;
            public bool Start(InteractionEvent e, InteractionReference reference)
            {
                if (!CanInteract(e) || !_beacon.IsServer)
                {
                    return false;
                }

                Entity user = _beacon.HeldHand(e).GetComponentInParent<Entity>();
                _beacon.ServerCallBomb(user != null ? user.transform : null);
                return false;
            }
        }
    }
}
