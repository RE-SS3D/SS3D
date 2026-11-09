using System.Collections.Generic;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions;
using SS3D.Systems.Interactions.UI;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Throw mode (R) and the server throw (Documents/architecture/2026-11_humanoid-animation-pass.md,
    /// Phase 3). The owner toggles throw mode; the next primary click throws the active hand's item at the
    /// point under the cursor. The server checks the throw, waits for the clip's release frame, and only
    /// then launches the item, so a knockdown, stun or hand swap before release keeps it in hand.
    /// </summary>
    public sealed partial class CombatInteractionNetwork
    {
        /// <summary>Extra facing hold after the release, so the follow-through does not swing away.</summary>
        private const float FacingHoldAfterRelease = 0.15f;
        private const float ReleaseClearanceRadius = 0.08f;

        private bool _throwModeActive;
        private bool _throwInputSubscribed;
        private Controls.HotkeysActions _throwHotkeys;
        private ThrowModeOverlayView _throwOverlay;
        private readonly List<Vector3> _throwArcWorld = new();
        private readonly List<Vector2> _throwArcScreen = new();
        private readonly List<Vector2> _throwRingScreen = new();

        private const int ArcSamples = 24;
        private const int RingSegments = 24;
        private const float RingRadius = 0.25f;

        private float _pendingThrowAt = -1f;
        private Hand _pendingThrowHand;
        private Item _pendingThrowItem;
        private Vector3 _pendingThrowTarget;

        public bool ThrowModeActive => _throwModeActive;

        /// <summary>Server: a throw is waiting for its release frame.</summary>
        public bool HasPendingThrow => _pendingThrowAt >= 0f;

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (IsOwner)
            {
                SubscribeThrowInput();
            }
            else
            {
                UnsubscribeThrowInput();
                SetThrowMode(false);
            }
        }

        protected override void OnDestroyed()
        {
            UnsubscribeThrowInput();
            _throwOverlay?.Detach();
            _throwOverlay = null;
            base.OnDestroyed();
        }

        private void SubscribeThrowInput()
        {
            if (_throwInputSubscribed || !SubSystems.TryGet(out InputSubSystem input))
            {
                return;
            }

            _throwHotkeys = input.Inputs.Hotkeys;
            _throwHotkeys.ThrowMode.performed += HandleThrowModePressed;
            _throwInputSubscribed = true;
        }

        private void UnsubscribeThrowInput()
        {
            if (!_throwInputSubscribed)
            {
                return;
            }

            _throwHotkeys.ThrowMode.performed -= HandleThrowModePressed;
            _throwInputSubscribed = false;
        }

        private void HandleThrowModePressed(InputAction.CallbackContext context)
        {
            if (IsOwner)
            {
                ToggleThrowMode();
            }
        }

        [Client]
        public void ToggleThrowMode() => SetThrowMode(!_throwModeActive);

        [Client]
        public void SetThrowMode(bool active)
        {
            _throwModeActive = active;
            if (!active)
            {
                _throwOverlay?.Hide();
            }
        }

        /// <summary>
        /// Primary click in throw mode. Returns true when the click was used (thrown, or throw mode
        /// switched off by an empty hand), so the normal interaction does not run as well.
        /// </summary>
        [Client]
        public bool TryRunThrowPrimary()
        {
            if (!_throwModeActive)
            {
                return false;
            }

            Hand hand = GetActiveHand();
            Item item = hand != null ? hand.ItemInHand : null;
            if (item == null)
            {
                SetThrowMode(false);
                return true;
            }

            if (TryGetThrowAim(out Vector3 aimPoint))
            {
                TryThrowAt(aimPoint);
            }

            return true;
        }

        /// <summary>
        /// Owner: throws the active hand's item at <paramref name="aimPoint"/> (clamped to its range), plays the
        /// throw at once and asks the server to launch at the release frame. Ends throw mode. Scenarios call this
        /// directly in place of a mouse click.
        /// </summary>
        [Client]
        public bool TryThrowAt(Vector3 aimPoint)
        {
            Hand hand = GetActiveHand();
            Item item = hand != null ? hand.ItemInHand : null;
            if (item == null || !CanThrow(hand, item, out _))
            {
                return false;
            }

            int variant = ChooseThrowVariant(item);
            Vector3 heading = aimPoint - transform.position;
            heading.y = 0f;
            if (heading.sqrMagnitude > 0.0001f && TryGetComponent(out HumanoidController controller))
            {
                float yaw = Quaternion.LookRotation(heading).eulerAngles.y;
                TryGetComponent(out HumanoidBodyStateMachine body);
                // Standing (or in combat, whose legs strafe) the body turns to the target. Walking in
                // Peaceful the legs keep the walk and the torso twists toward the target instead
                // (HumanoidIkController), so every peer gets the target yaw as aim.
                if (!controller.HasMovementInput || (body != null && body.CombatMode.IsCombat()))
                {
                    controller.HoldFacing(yaw, ThrowAnimation.ReleaseSeconds(variant) + FacingHoldAfterRelease);
                }

                body?.SubmitAim(yaw, 0f);
                GetComponentInChildren<HumanoidIkController>()?.SetThrowAimYaw(yaw);
            }

            if (TryGetComponent(out HumanoidCombatController combat))
            {
                combat.RequestAttack(AnimationTriggerId.Throw, 0f, (byte)variant);
            }

            CmdThrow(aimPoint);
            SetThrowMode(false);
            return true;
        }

        /// <summary>
        /// Owner: draws where a throw would go right now, the flight arc from the release point to the first
        /// thing it hits, and a ring there. Past the item's range the arc ends short of the cursor.
        /// </summary>
        [Client]
        private void TickThrowMode()
        {
            if (!_throwModeActive)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetThrowMode(false);
                return;
            }

            Camera camera = Camera.main;
            if (!EnsureThrowOverlay() || camera == null || !TryGetThrowAim(out Vector3 aimPoint))
            {
                _throwOverlay?.Hide();
                return;
            }

            _throwArcScreen.Clear();
            _throwRingScreen.Clear();
            Hand hand = GetActiveHand();
            Item item = hand != null ? hand.ItemInHand : null;
            if (item == null || !CanThrow(hand, item, out float range))
            {
                AddRing(camera, aimPoint, _throwRingScreen);
                _throwOverlay.Show(_throwArcScreen, _throwRingScreen, ThrowModeOverlayView.State.Blocked);
                return;
            }

            Vector3 target = ResolveLandingPoint(aimPoint, range, out bool clamped);
            Vector3 feet = transform.position;
            Vector3 heading = target - feet;
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.0001f)
            {
                heading = transform.forward;
            }

            Vector3 origin = ClearReleasePoint(feet, ThrowRules.ReleasePoint(feet, heading, hand.Side == HandSide.Left));
            Vector3 landing = SampleFlight(origin, target, _throwArcWorld);
            foreach (Vector3 point in _throwArcWorld)
            {
                AddScreenPoint(camera, point, _throwArcScreen);
            }

            AddRing(camera, landing, _throwRingScreen);
            _throwOverlay.Show(_throwArcScreen, _throwRingScreen,
                clamped ? ThrowModeOverlayView.State.Clamped : ThrowModeOverlayView.State.InRange);
        }

        /// <summary>
        /// Samples the server's launch (same origin, velocity and gravity) into <paramref name="points"/> and
        /// stops at the first collider in the way. Returns where the flight ends.
        /// </summary>
        private Vector3 SampleFlight(Vector3 origin, Vector3 target, List<Vector3> points)
        {
            points.Clear();
            float flight = ThrowRules.FlightSeconds(origin, target);
            Vector3 velocity = ThrowRules.LaunchVelocity(origin, target, Physics.gravity, flight);
            int mask = LayerMask.GetMask("Default");
            Vector3 previous = origin;
            points.Add(origin);
            for (int i = 1; i <= ArcSamples; i++)
            {
                float t = flight * i / ArcSamples;
                Vector3 point = origin + velocity * t + 0.5f * t * t * Physics.gravity;
                Vector3 step = point - previous;
                float length = step.magnitude;
                if (length > 0.0001f && TryFirstHit(previous, step / length, length, mask, out RaycastHit hit))
                {
                    points.Add(hit.point);
                    return hit.point;
                }

                points.Add(point);
                previous = point;
            }

            return previous;
        }

        private bool TryFirstHit(Vector3 from, Vector3 direction, float length, int mask, out RaycastHit first)
        {
            first = default;
            float nearest = float.MaxValue;
            foreach (RaycastHit hit in Physics.RaycastAll(from, direction, length, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != null && !hit.transform.IsChildOf(transform) && hit.distance < nearest)
                {
                    nearest = hit.distance;
                    first = hit;
                }
            }

            return nearest < float.MaxValue;
        }

        private static void AddRing(Camera camera, Vector3 centre, List<Vector2> screen)
        {
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                AddScreenPoint(camera, centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * RingRadius, screen);
            }
        }

        private static void AddScreenPoint(Camera camera, Vector3 world, List<Vector2> screen)
        {
            Vector3 point = camera.WorldToScreenPoint(world);
            if (point.z > 0f)
            {
                screen.Add(new Vector2(point.x, point.y));
            }
        }

        private bool EnsureThrowOverlay()
        {
            if (_throwOverlay != null)
            {
                return true;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem uiShell) || !uiShell.TryGetLayer(UiLayer.Overlay, out VisualElement layer))
            {
                return false;
            }

            StyleSheet style = SubSystems.TryGet(out ArmedInteractionSubSystem armed) ? armed.OverlayStyleSheet : null;
            _throwOverlay = new ThrowModeOverlayView(style);
            _throwOverlay.Attach(layer);
            return true;
        }

        private bool TryGetThrowAim(out Vector3 aimPoint)
        {
            aimPoint = default;
            return TryGetComponent(out HumanoidController controller) && controller.TryGetCombatAimRay(out _, out aimPoint);
        }

        [ServerRpc]
        private void CmdThrow(Vector3 aimPoint)
        {
            if (HasPendingThrow)
            {
                return;
            }

            Hand hand = GetActiveHand();
            Item item = hand != null ? hand.ItemInHand : null;
            if (item == null || !CanThrow(hand, item, out float range))
            {
                return;
            }

            int variant = ChooseThrowVariant(item);
            _pendingThrowHand = hand;
            _pendingThrowItem = item;
            _pendingThrowTarget = ResolveLandingPoint(aimPoint, range, out _);
            _pendingThrowAt = Time.time + ThrowAnimation.ReleaseSeconds(variant);
        }

        [Server]
        public void ClearPendingThrow()
        {
            _pendingThrowAt = -1f;
            _pendingThrowHand = null;
            _pendingThrowItem = null;
        }

        private void HandleServerThrowUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (!IsServer || !HasPendingThrow || Time.time < _pendingThrowAt)
            {
                return;
            }

            Hand hand = _pendingThrowHand;
            Item item = _pendingThrowItem;
            Vector3 target = _pendingThrowTarget;
            ClearPendingThrow();

            if (!IsThrowStillValid(hand, item))
            {
                return;
            }

            ServerLaunch(hand, item, target);
        }

        /// <summary>
        /// Decision 13: an interrupted throw keeps the item. Knocked down, stunned (arms unusable,
        /// staggered), swapped hands, or the item already left the hand: no launch.
        /// </summary>
        [Server]
        private bool IsThrowStillValid(Hand hand, Item item)
        {
            return hand != null && item != null
                && hand.ItemInHand == item
                && GetActiveHand() == hand
                && CanThrow(hand, item, out _);
        }

        [Server]
        private void ServerLaunch(Hand hand, Item item, Vector3 target)
        {
            Vector3 feet = transform.position;
            Vector3 heading = target - feet;
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.0001f)
            {
                heading = transform.forward;
            }

            Vector3 origin = ClearReleasePoint(feet, ThrowRules.ReleasePoint(feet, heading, hand.Side == HandSide.Left));
            hand.PlaceHeldItemOutOfHand(origin, Quaternion.LookRotation(heading.normalized));

            if (item.TryGetComponent(out Rigidbody body))
            {
                body.isKinematic = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                Vector3 gravity = body.useGravity ? Physics.gravity : Vector3.zero;
                float flight = ThrowRules.FlightSeconds(item.transform.position, target);
                body.linearVelocity = ThrowRules.LaunchVelocity(item.transform.position, target, gravity, flight);
                body.angularVelocity = Vector3.Cross(Vector3.up, heading.normalized) * 6f;
            }

            foreach (IThrowReleaseHandler handler in item.GetComponents<IThrowReleaseHandler>())
            {
                handler.ServerOnThrown();
            }
        }

        /// <summary>Pulls the release point back toward the chest when a wall stands in front of the thrower.</summary>
        private Vector3 ClearReleasePoint(Vector3 feet, Vector3 release)
        {
            Vector3 chest = new(feet.x, release.y, feet.z);
            Vector3 toRelease = release - chest;
            float distance = toRelease.magnitude;
            if (distance < 0.001f)
            {
                return release;
            }

            Vector3 direction = toRelease / distance;
            RaycastHit[] hits = Physics.SphereCastAll(chest, ReleaseClearanceRadius, direction, distance,
                LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
            float clear = distance;
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform != null && !hit.transform.IsChildOf(transform) && hit.distance < clear)
                {
                    clear = Mathf.Max(0f, hit.distance - 0.01f);
                }
            }

            return chest + direction * clear;
        }

        /// <summary>
        /// Shared client/server check: a usable hand, a body that can act, a throwable weight, and both
        /// hands free for a two-handed item. Range is 0 when the answer is no.
        /// </summary>
        private bool CanThrow(Hand hand, Item item, out float range)
        {
            range = 0f;
            if (hand == null || item == null || !hand.CanUseHand)
            {
                return false;
            }

            if (TryGetComponent(out HumanoidBodyStateMachine body) && !body.CanPerformAction())
            {
                return false;
            }

            if (TwoHandedWeaponRules.RequiresBothHands(item, out _) && hand.HandsController is Hands hands)
            {
                foreach (Hand other in hands.PlayerHands)
                {
                    if (other != hand && !other.IsEmpty())
                    {
                        return false;
                    }
                }
            }

            range = ThrowRules.MaxRange(ThrowRules.ThrownWeight(item));
            return range > 0f;
        }

        private Vector3 ResolveLandingPoint(Vector3 aimPoint, float range, out bool clamped)
        {
            Vector3 landing = ThrowRules.ClampToRange(transform.position, aimPoint, range, out clamped);
            if (clamped && Physics.Raycast(landing + Vector3.up * 1.5f, Vector3.down, out RaycastHit floor, 4f,
                    LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                landing = floor.point;
            }

            return landing;
        }

        private Hand GetActiveHand()
        {
            Hands hands = GetComponent<Hands>();
            return hands != null ? hands.SelectedHand : null;
        }

        private static int ChooseThrowVariant(Item item)
        {
            foreach (IThrowReleaseHandler handler in item.GetComponents<IThrowReleaseHandler>())
            {
                if (handler.UsesGrenadeThrow)
                {
                    return ThrowAnimation.Grenade;
                }
            }

            return ThrowAnimation.Overhand;
        }
    }
}
