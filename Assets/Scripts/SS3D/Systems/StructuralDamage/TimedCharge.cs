using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.StructuralDamage
{
    public enum ExplosiveKind { C4, X4, Minibomb }

    /// <summary>Server-owned explosive fuse; clients seek one countdown recording from the replicated deadline.</summary>
    public sealed class TimedCharge : InteractionTargetNetworkBehaviour, IInteractionSourceExtension
    {
        public const float FuseSeconds = 15f;
        public const float DefuseSeconds = 3f;
        [SyncVar] private bool _armed;
        [SyncVar] private double _deadline;
        [SyncVar] private bool _attached;
        [SerializeField] private ExplosiveKind _kind;
        [SerializeField] private ChargeCountdownDisplay _display;
        [SerializeField] private string _countdownId;
        [SerializeField] private string _confirmId;
        [SerializeField] private Object _wirecuttersAsset;
        private bool _spent;
        private AudioSource _countdown;
        private double _playingDeadline;
        private TileCoord _targetTile;
        private Vector2 _coneDirection;

        private double Now
        {
            get
            {
                NetworkObject network = GetComponent<NetworkObject>();
                FishNet.Managing.Timing.TimeManager clock = network != null ? network.TimeManager : null;
                return clock != null ? clock.TicksToTime(clock.Tick) : 0d;
            }
        }
        public ExplosiveKind Kind => _kind;
        public float FuseDuration => _kind == ExplosiveKind.Minibomb ? 5f : FuseSeconds;
        public bool Armed => _armed;
        public float Remaining => _armed ? Mathf.Max(0f, (float)(_deadline - Now)) : FuseDuration;
        public string Label => _kind == ExplosiveKind.Minibomb ? "minibomb" : _kind.ToString();

        public override IInteraction[] CreateTargetInteractions(InteractionEvent e) =>
            new IInteraction[] { new ArmInteraction(this), new DefuseInteraction(this) };

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries, InteractionEvent context)
        {
            if (HeldHand(context) == null) return;
            if (_kind == ExplosiveKind.Minibomb)
            {
                entries.Add(InteractionEntry.SourceOnly(new ThrowInteraction(this)));
                return;
            }
            if (_armed || _spent) return;
            HashSet<PlacedTileObject> seen = new();
            foreach (IInteractionTarget target in targets)
            {
                InteractionEvent candidate = context.WithTarget(target);
                PlacedTileObject placed = AttachmentTarget(candidate);
                if (placed == null || !seen.Add(placed)) continue;
                entries.Add(new InteractionEntry(target, new AttachInteraction(this)));
            }
        }

        private void Update()
        {
            if (!GetComponent<NetworkObject>().IsSpawned) return;
            if (IsServer && _attached && GetComponent<Item>().Container != null) _attached = false;
            if (_attached && GetComponent<Item>().Container == null)
                GetComponent<Rigidbody>().isKinematic = true;
            if (IsServer && _armed && !_spent && Now >= _deadline)
            {
                Detonate();
                return;
            }
            if (!IsClient) return;
            _display?.Show(Remaining, _armed, Now);
            if (!_armed) { StopCountdown(); return; }
            if (Remaining <= 0f || _playingDeadline == _deadline) return;
            if (SubSystems.TryGet(out AudioSubSystem audio))
            {
                _countdown = audio.PlayAudioSourceLocal(AudioType.Sfx, _countdownId, transform.position,
                    GetComponent<NetworkObject>(), volume: 0.7f, minRange: 1f, maxRange: 12f,
                    startTime: Mathf.Max(0f, FuseDuration - Remaining));
                if (_countdown != null) _playingDeadline = _deadline;
            }
        }

        public override void OnStopClient()
        {
            StopCountdown();
            base.OnStopClient();
        }

        private void StopCountdown()
        {
            if (_countdown != null && _countdown.transform.parent == transform)
            {
                _countdown.Stop();
                _countdown.transform.SetParent(null);
            }
            _countdown = null;
            _playingDeadline = 0d;
        }

        [Server]
        private void Arm()
        {
            if (_armed || _spent) return;
            _deadline = Now + FuseDuration;
            _armed = true;
        }

        [Server]
        private void Defuse()
        {
            if (!_armed || _spent || Now >= _deadline || _kind == ExplosiveKind.Minibomb) return;
            _armed = false;
            RpcConfirm();
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcConfirm()
        {
            StopCountdown();
            if (SubSystems.TryGet(out AudioSubSystem audio))
                audio.PlayAudioSourceLocal(AudioType.Sfx, _confirmId, transform.position, null,
                    volume: 0.65f, pitch: 0.85f, minRange: 1f, maxRange: 12f);
        }

        [Server]
        private void Detonate()
        {
            _spent = true;
            _armed = false;
            if (SubSystems.TryGet(out TileSubSystem tiles) && tiles.CurrentMap != null
                && tiles.QueryService != null && SubSystems.TryGet(out StructuralDamageSubSystem damage))
            {
                TileCoord tile = _attached ? _targetTile
                    : tiles.QueryService.WorldToTile(transform.position, tiles.CurrentMap.MapId);
                if (_kind == ExplosiveKind.X4 && _attached)
                    damage.ResolveShapedCharge(tile, _coneDirection);
                else
                    damage.ResolveBlast(tile, _kind == ExplosiveKind.Minibomb ? 90f : _kind == ExplosiveKind.X4 ? 240f : 180f,
                        _kind == ExplosiveKind.X4 ? 60f : 45f, _kind == ExplosiveKind.Minibomb);
            }
            Item item = GetComponent<Item>();
            if (item.Container != null) item.Container.RemoveItem(item);
            ServerManager.Despawn(gameObject);
        }

        private bool HasCutters(InteractionEvent e)
        {
            Item tool = e.Source.GetComponent<Item>();
            return tool != null && tool.Asset != null && tool.Asset == _wirecuttersAsset;
        }

        private Hand HeldHand(InteractionEvent e)
        {
            Hand hand = e?.Source?.GetRootSource() as Hand;
            return hand != null && hand.ItemInHand == GetComponent<Item>() ? hand : null;
        }

        private static PlacedTileObject AttachmentTarget(InteractionEvent e) =>
            TryAttachmentSurface(e, out PlacedTileObject target, out _, out _) ? target : null;

        private static bool TryAttachmentSurface(InteractionEvent e, out PlacedTileObject target,
            out Vector3 point, out Vector3 normal)
        {
            target = null;
            point = default;
            normal = default;
            if (e?.Target is not IGameObjectProvider provider || !e.HasPoint) return false;
            target = provider.GameObject.GetComponentInParent<PlacedTileObject>();
            if (target == null || target.Layer != TileLayer.Turf
                || (target.GenericType != TileObjectGenericType.Wall && target.GenericType != TileObjectGenericType.Door))
                return false;
            // Interaction RPC sends a point, not the client normal. Recompute the contact on both peers.
            IInteractionSource root = e.Source.GetRootSource();
            Vector3 origin = root is IInteractionOriginProvider reach ? reach.InteractionOrigin : root.GameObject.transform.position;
            Vector3 delta = e.Point - origin;
            if (delta.sqrMagnitude < 0.001f) return false;
            Ray ray = new(origin, delta.normalized);
            float nearest = delta.magnitude + 0.25f;
            bool found = false;
            foreach (Collider collider in target.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger || !collider.enabled || StationBounds.IsCeiling(collider)
                    || !collider.Raycast(ray, out RaycastHit hit, nearest)) continue;
                nearest = hit.distance;
                point = hit.point;
                normal = hit.normal;
                found = true;
            }
            return found && Vector3.Distance(point, e.Point) < 0.25f
                && new Vector2(normal.x, normal.z).sqrMagnitude > 0.5f;
        }

        private sealed class ArmInteraction : IInteraction
        {
            private readonly TimedCharge _charge;
            public ArmInteraction(TimedCharge charge) => _charge = charge;
            public int Priority => _charge._kind == ExplosiveKind.Minibomb ? 40 : 15;
            public string GetName(InteractionEvent e) => $"Arm {_charge.Label} ({_charge.FuseDuration:0} seconds)";
            public string GetGenericName() => "ArmC4";
            public Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Power;
            public bool CanInteract(InteractionEvent e) => _charge != null && !_charge._armed && !_charge._spent
                && (_charge._kind != ExplosiveKind.X4 || _charge._attached)
                && (_charge.GetComponent<Item>().Container == null
                    || (_charge._kind == ExplosiveKind.Minibomb && _charge.HeldHand(e) != null))
                && InteractionExtensions.RangeCheck(e);
            public bool Start(InteractionEvent e, InteractionReference reference)
            {
                if (CanInteract(e)) _charge.Arm();
                return false;
            }
        }

        private sealed class AttachInteraction : IInteraction
        {
            private readonly TimedCharge _charge;
            public AttachInteraction(TimedCharge charge) => _charge = charge;
            public int Priority => 40;
            public string GetName(InteractionEvent e) => $"Attach and arm {_charge.Label}";
            public string GetGenericName() => "AttachCharge";
            public Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Power;
            public bool CanInteract(InteractionEvent e) => _charge != null && !_charge._armed && !_charge._spent
                && _charge.HeldHand(e) != null && AttachmentTarget(e) != null && InteractionExtensions.RangeCheck(e);
            public bool Start(InteractionEvent e, InteractionReference reference)
            {
                if (!CanInteract(e) || !SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null) return false;
                if (!TryAttachmentSurface(e, out PlacedTileObject target, out Vector3 contact, out Vector3 surfaceNormal)) return false;
                Vector3 normal = new Vector3(surfaceNormal.x, 0f, surfaceNormal.z).normalized;
                _charge._targetTile = tiles.QueryService.WorldToTile(target.transform.position, tiles.CurrentMap.MapId);
                _charge._coneDirection = new Vector2(-normal.x, -normal.z);
                Item item = _charge.GetComponent<Item>();
                // The charge's broad back is -Y; its readout (+Y) faces the person attaching it.
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
                float thickness = item.GetComponent<BoxCollider>().size.y;
                _charge.HeldHand(e).PlaceHeldItemOutOfHand(contact + normal * (thickness * 0.5f + 0.01f), rotation);
                // ItemUtility.Place lifts floor drops; a wall charge needs the exact contact pose.
                ItemUtility.SetWorldPose(item.transform, contact + normal * (thickness * 0.5f + 0.01f), rotation);
                item.GetComponent<Rigidbody>().isKinematic = true;
                _charge._attached = true;
                _charge.Arm();
                return false;
            }
        }

        private sealed class ThrowInteraction : IInteraction, ITargetedInteraction
        {
            private readonly TimedCharge _charge;
            public ThrowInteraction(TimedCharge charge) => _charge = charge;
            public int Priority => 10;
            public string GetName(InteractionEvent e) => "Throw minibomb";
            public string GetGenericName() => "ThrowMinibomb";
            public Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Discard;
            public InteractionTier GetTier(InteractionEvent e) => InteractionTier.Targeted;
            public bool CanInteract(InteractionEvent e) => _charge != null && !_charge._spent && _charge.HeldHand(e) != null;
            public bool CanTarget(InteractionEvent origin, InteractionEvent target) => CanInteract(origin)
                && target.HasPoint && Vector3.Distance(origin.Source.GetRootSource().GameObject.transform.position, target.Point) <= 10f;
            public bool Start(InteractionEvent e, InteractionReference reference)
            {
                if (!CanInteract(e) || !CanTarget(e, e)) return false;
                Hand hand = _charge.HeldHand(e);
                Transform person = hand.GetComponentInParent<HumanInventory>().transform;
                Vector3 origin = person.position + Vector3.up * 1.2f;
                float clearance = Physics.SphereCast(origin, 0.08f, person.forward, out RaycastHit obstacle,
                    0.45f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore)
                    ? Mathf.Max(0f, obstacle.distance - 0.01f) : 0.45f;
                origin += person.forward * clearance;
                float flight = Mathf.Clamp(Vector3.Distance(origin, e.Point) / 8f, 0.3f, 1.2f);
                hand.PlaceHeldItemOutOfHand(origin, Quaternion.identity);
                Rigidbody body = _charge.GetComponent<Rigidbody>();
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                Vector3 gravity = body.useGravity ? Physics.gravity : Vector3.zero;
                body.linearVelocity = (e.Point - origin) / flight - gravity * (0.5f * flight);
                _charge.Arm();
                return false;
            }
        }

        private sealed class DefuseInteraction : DelayedInteraction, IInteraction
        {
            private readonly TimedCharge _charge;
            public DefuseInteraction(TimedCharge charge) { _charge = charge; Delay = DefuseSeconds; CheckInterval = 0.1f; }
            public int Priority => 40;
            public override string GetName(InteractionEvent e) => $"Defuse {_charge.Label} (wirecutters)";
            public override string GetGenericName() => "DefuseC4";
            public override Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Power;
            public override bool CanInteract(InteractionEvent e) => _charge != null && _charge._kind != ExplosiveKind.Minibomb
                && _charge._armed && !_charge._spent && _charge.Remaining > 0f && _charge.HasCutters(e)
                && InteractionExtensions.RangeCheck(e);
            protected override void StartDelayed(InteractionEvent e, InteractionReference reference) => _charge.Defuse();
            public override void Cancel(InteractionEvent e, InteractionReference reference) { }
        }
    }
}
