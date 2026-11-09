using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// The replaceable bulb or tube in a light fixture. A shot breaks it, an empty hand takes it out and a
    /// matching <see cref="LightBulbItem"/> puts a new one in. Only an intact bulb lets <see cref="LightPower"/> emit.
    /// </summary>
    public sealed class LightFixtureBulb : InteractionTargetNetworkBehaviour
    {
        private const float BreakVolume = 0.8f;
        private const float BreakMinRange = 2f;
        private const float BreakMaxRange = 25f;
        private const float SocketVolume = 0.5f;
        private const float SocketMinRange = 1f;
        private const float SocketMaxRange = 8f;

        /// <summary>
        /// How far behind a wall collider's face a mounted bulb's glass can sit. Walls collide as full tiles, thicker
        /// than they look, so the glass is inside the block; a fixture on the wall's far side is about 0.8 m in.
        /// </summary>
        public const float WallInset = 0.35f;

        /// <summary>Bulbs are small; widen the hit box so a round that grazes the glass still counts.</summary>
        private const float HitPadding = 0.03f;

        private static readonly List<LightFixtureBulb> Registered = new();
        private static readonly string[] SocketClip = { LightFixtureAudioTrackIds.Socket };

        [SerializeField]
        private LightBulbKind _kind;
        [SerializeField]
        private LightPower _lightPower;
        [SerializeField]
        private GameObject _intactBulb;
        [SerializeField]
        private GameObject _brokenBulb;
        [SerializeField]
        private ObjectAssetReference _intactItem;
        [SerializeField]
        private ObjectAssetReference _brokenItem;

        [SyncVar(OnChange = nameof(SyncState))]
        private LightBulbState _state;

        private Renderer _intactRenderer;

        /// <summary>Every enabled fixture, for hitscan tests against the glass.</summary>
        public static IReadOnlyList<LightFixtureBulb> All => Registered;

        public LightBulbKind Kind => _kind;

        public LightBulbState State => _state;

        public bool IsIntact => _state == LightBulbState.Intact;

        protected override void OnEnabled()
        {
            base.OnEnabled();
            Registered.Add(this);
            ApplyVisual();
        }

        protected override void OnDisabled()
        {
            Registered.Remove(this);
            base.OnDisabled();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyVisual();
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new RemoveLightBulbInteraction() };
        }

        /// <summary>
        /// World-space box around the intact glass, or false when there is none to hit.
        /// </summary>
        public bool TryGetGlassBounds(out Bounds bounds)
        {
            bounds = default;
            if (!IsIntact || _intactBulb == null)
            {
                return false;
            }

            if (_intactRenderer == null)
            {
                _intactRenderer = _intactBulb.GetComponent<Renderer>();
            }

            if (_intactRenderer == null)
            {
                return false;
            }

            bounds = _intactRenderer.bounds;
            bounds.Expand(HitPadding * 2f);
            return true;
        }

        /// <summary>
        /// The nearest whole bulb <paramref name="shot"/> passes through before <paramref name="stopDistance"/>.
        /// </summary>
        public static bool TryFindStruck(Ray shot, float stopDistance, out LightFixtureBulb struck)
        {
            struck = null;
            float nearest = stopDistance;
            foreach (LightFixtureBulb fixture in Registered)
            {
                if (fixture.TryGetGlassBounds(out Bounds glass) && LightBulbRules.CrossesBefore(shot, glass, ref nearest))
                {
                    struck = fixture;
                }
            }

            return struck != null;
        }

        /// <summary>Server: shatter an intact bulb. False when there was nothing to break.</summary>
        [Server]
        public bool ServerBreak()
        {
            if (!IsIntact)
            {
                return false;
            }

            Vector3 glass = SocketPosition();
            SetState(LightBulbState.Broken);
            PlaySound(LightFixtureAudioTrackIds.BulbBreak, glass, BreakVolume, BreakMinRange, BreakMaxRange,
                Random.Range(1.05f, 1.2f));
            return true;
        }

        /// <summary>
        /// Server: take the bulb out into <paramref name="hand"/>. An intact bulb comes out whole, a broken one as
        /// a broken bulb.
        /// </summary>
        [Server]
        public bool ServerRemoveInto(Hand hand)
        {
            if (_state == LightBulbState.Empty || hand == null || !hand.IsEmpty())
            {
                return false;
            }

            ObjectAssetReference removed = IsIntact ? _intactItem : _brokenItem;
            SetState(LightBulbState.Empty);
            if (removed != null && SubSystems.TryGet(out ItemSubSystem items))
            {
                Item prefab = Assets.Get<Item>(removed);
                if (prefab != null)
                {
                    items.SpawnItemInContainer(prefab.GameObject, hand.Container);
                }
            }

            PlaySocketClick();
            return true;
        }

        /// <summary>
        /// Server: fit <paramref name="bulb"/>. A broken bulb still in the socket drops out onto the floor.
        /// </summary>
        [Server]
        public bool ServerInsert(Item bulb)
        {
            if (IsIntact || bulb == null)
            {
                return false;
            }

            if (_state == LightBulbState.Broken)
            {
                DropBrokenBulb();
            }

            bulb.Delete();
            SetState(LightBulbState.Intact);
            PlaySocketClick();
            return true;
        }

        /// <summary>Server / EditMode: set the socket state and refresh the fixture.</summary>
        public void SetState(LightBulbState state)
        {
            // Never assign the SyncVar on a pure client, nor before the NetworkObject exists (EditMode).
            if (NetworkObject != null && NetworkObject.IsSpawned && !IsServer)
            {
                return;
            }

            _state = state;
            ApplyVisual();
            ResolveLightPower()?.RefreshVisuals();
        }

        private void SyncState(LightBulbState previous, LightBulbState next, bool asServer)
        {
            if (asServer)
            {
                return;
            }

            ApplyVisual();
            ResolveLightPower()?.RefreshVisuals();
        }

        private void ApplyVisual()
        {
            if (_intactBulb != null)
            {
                _intactBulb.SetActive(_state == LightBulbState.Intact);
            }

            if (_brokenBulb != null)
            {
                _brokenBulb.SetActive(_state == LightBulbState.Broken);
            }
        }

        private LightPower ResolveLightPower()
        {
            if (_lightPower == null)
            {
                TryGetComponent(out _lightPower);
            }

            return _lightPower;
        }

        private void DropBrokenBulb()
        {
            if (_brokenItem == null || !SubSystems.TryGet(out ItemSubSystem items))
            {
                return;
            }

            // Out of the socket and away from the wall, so it falls clear of the fixture.
            items.SpawnItem(_brokenItem.Id, SocketPosition() + transform.forward * 0.15f, Quaternion.identity);
        }

        private void PlaySocketClick()
        {
            PlaySound(SocketClip, SocketPosition(), SocketVolume,
                SocketMinRange, SocketMaxRange, 1.3f);
        }

        /// <summary>
        /// The socket's world centre: the bulb meshes sit up the wall, not at the tile origin. Read from the mesh,
        /// because a deactivated renderer reports empty bounds.
        /// </summary>
        private Vector3 SocketPosition()
        {
            GameObject socket = _intactBulb != null ? _intactBulb : _brokenBulb;
            MeshFilter filter = socket != null ? socket.GetComponent<MeshFilter>() : null;
            return filter != null && filter.sharedMesh != null
                ? socket.transform.TransformPoint(filter.sharedMesh.bounds.center)
                : transform.position;
        }

        private static void PlaySound(string[] clips, Vector3 position, float volume, float minRange, float maxRange, float pitch)
        {
            if (clips.Length == 0 || !SubSystems.TryGet(out AudioSubSystem audio))
            {
                return;
            }

            audio.PlayAudioSource(AudioType.Sfx, clips[Random.Range(0, clips.Length)], position, null, false, volume,
                pitch, minRange, maxRange);
        }
    }
}
