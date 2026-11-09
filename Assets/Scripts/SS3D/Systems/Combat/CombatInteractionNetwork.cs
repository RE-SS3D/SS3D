using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using FishNet.Object;
using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Audio;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Stamina;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// NetworkActor sibling on Human: melee/ranged Harm-primary RPCs and aim sync.
    /// Intent and delayed-interaction tracking stay on <see cref="InteractionController"/>.
    /// </summary>
    [RequireComponent(typeof(InteractionController))]
    public sealed class CombatInteractionNetwork : NetworkActor
    {
        [SerializeField] private InteractionController _interactionController;

        private Vector3 _meleeAimRayOrigin;
        private Vector3 _meleeAimPoint;
        private bool _hasMeleeAimRay;

        /// <summary>Server-scheduled Harm primary connect (bypasses DelayedInteraction.Update).</summary>
        private float _pendingMeleeConnectAt = -1f;
        private Hand _pendingMeleeHand;
        private MeleeWeaponProfile _pendingMeleeProfile;
        private int _meleeSwingSerial;

        protected override void OnAwake()
        {
            base.OnAwake();

            if (_interactionController == null)
            {
                _interactionController = GetComponent<InteractionController>();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            // Owner-only Update() never runs on a dedicated server — schedule melee connect here.
            AddHandle(UpdateEvent.AddListener(HandleServerMeleeConnectUpdate));
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            TrySyncMeleeAimDuringSwing();
        }

        /// <summary>
        /// Starts windup/swing/recovery for Harm primary regardless of hover target.
        /// Damage (if any) is applied at connect from synced aim.
        /// </summary>
        [Client]
        public bool TryRunMeleeSwingPrimary()
        {
            if (!TryCreateMeleeHitInteraction(out MeleeHitInteraction hit, out Hand hand))
            {
                return false;
            }

            if (!hit.CanStartSwing(hand))
            {
                return false;
            }

            // Optimistic busy lock is for pure clients (Cmd latency). On host/listen-server the same
            // Hand tracker is shared: locking before Cmd makes server CanStartSwing fail immediately
            // (cooldown UI, no connect / hitmarker). ServerBeginSwing + recovery TargetRpc lock instead.
            if (!IsServer)
            {
                BeginLocalSwingCycle(hand, hit.Profile);
            }

            TryPlayMeleeSwingTelegraph(hit, hand);
            TrySyncMeleeAimToServer();
            CmdRunMeleeSwing();
            return true;
        }

        /// <summary>
        /// Harm primary fire when the selected hand holds a <see cref="RangedWeaponItemExtension"/>.
        /// Instant hitscan — no windup; reload/cooldown pace the gun.
        /// </summary>
        [Client]
        public bool TryRunRangedFirePrimary()
        {
            if (!TryGetHeldRangedWeapon(out Hand hand, out RangedWeaponItemExtension weapon))
            {
                return false;
            }

            weapon.ServerCompleteReloadIfDue();

            // Empty mag → server dry-fire (+ reload if possible); do not fall through to melee.
            if (weapon.RoundsRemaining <= 0)
            {
                bool startingReload = weapon.CanStartReload();
                if (!IsServer && startingReload)
                {
                    weapon.BeginLocalReload(weapon.Profile.ReloadSeconds);
                }

                if (startingReload)
                {
                    TryPlayRangedReloadTelegraph();
                }

                TrySyncMeleeAimToServer();
                CmdRunRangedFire();
                return true;
            }

            if (!weapon.CanStartFire())
            {
                return true;
            }

            if (!IsServer)
            {
                weapon.BeginLocalFireCooldown();
            }

            TryPlayRangedFireTelegraph();
            TrySyncMeleeAimToServer();
            CmdRunRangedFire();
            return true;
        }

        [Client]
        public bool TryRunRangedReloadPrimary()
        {
            if (!TryGetHeldRangedWeapon(out _, out RangedWeaponItemExtension weapon))
            {
                return false;
            }

            if (!weapon.CanStartReload())
            {
                return false;
            }

            if (!IsServer)
            {
                weapon.BeginLocalReload(weapon.Profile.ReloadSeconds);
            }

            TryPlayRangedReloadTelegraph();
            CmdRunRangedReload();
            return true;
        }

        [ServerOrClient]
        private bool TryGetHeldRangedWeapon(out Hand hand, out RangedWeaponItemExtension weapon)
        {
            Hands hands = GetComponent<Hands>();
            return TwoHandedWeaponRules.TryGetWieldedRangedWeapon(hands, out hand, out weapon)
                && hand.CanUseHand;
        }

        [ServerRpc]
        private void CmdRunRangedFire()
        {
            if (_interactionController.CurrentIntent != IntentType.Harm)
            {
                _interactionController.RejectInteractionForOwner();
                return;
            }

            if (!TryGetHeldRangedWeapon(out _, out RangedWeaponItemExtension weapon))
            {
                _interactionController.RejectInteractionForOwner();
                return;
            }

            weapon.ServerCompleteReloadIfDue();

            if (weapon.RoundsRemaining <= 0)
            {
                PlayGunEmptySound(weapon);
                if (weapon.ServerTryBeginReload())
                {
                    ServerNotifyRangedReloadStarted(weapon);
                }

                return;
            }

            if (!weapon.CanStartFire() || !weapon.ServerTryConsumeRound())
            {
                _interactionController.RejectInteractionForOwner();
                return;
            }

            StaminaController stamina = GetComponent<StaminaController>();
            if (weapon.Profile.StaminaCost > 0f)
            {
                stamina?.ServerDepleteStamina(weapon.Profile.StaminaCost);
            }

            if (!TryGetMeleeAimRay(out Ray aimRay))
            {
                Entity entity = GetComponent<Entity>();
                Vector3 origin = entity != null
                    ? entity.transform.position + Vector3.up * 1.5f
                    : transform.position + Vector3.up * 1.5f;
                Vector3 direction = entity != null ? entity.transform.forward : transform.forward;
                aimRay = new Ray(origin, direction);
            }

            weapon.GetMuzzleWorldPose(out Vector3 firePosition, out _);
            PlayGunfireSound(firePosition, weapon.Profile.VisualStyle);

            float maxRange = Mathf.Max(1f, weapon.Profile.MaxRangeMeters);
            float aimDistance = maxRange;
            if (Physics.Raycast(aimRay, out RaycastHit aimHit, maxRange, StationBounds.WithoutCeiling(~0), QueryTriggerInteraction.Ignore))
            {
                aimDistance = aimHit.distance;
            }

            float horizontalSpeed = GetHorizontalMoveSpeed();
            float exertionPenalty = stamina?.ExertionPenalty ?? 0f;
            float spread = weapon.CurrentSpreadDegrees(horizontalSpeed, aimDistance, exertionPenalty);
            var rng = new System.Random(unchecked(Environment.TickCount ^ GetInstanceID() ^ weapon.RoundsRemaining));

            HumanHealthController selfHealth = GetComponentInChildren<HumanHealthController>();
            bool resolved = RangedHitscanResolver.TryResolveShot(
                aimRay,
                weapon.Profile,
                spread,
                rng,
                selfHealth,
                out HumanHealthController health,
                out BodyZone zone,
                out TileCoord structuralCoord,
                out bool hitLiving,
                out bool hitStructural,
                out Vector3 impactPoint,
                out Vector3 impactNormal,
                out bool hasImpact,
                out Vector3 shotDirection);

            bool landed = false;
            if (resolved && hitLiving && health != null)
            {
                ServerApplyLivingHit(weapon.Profile, health, zone);
                if (weapon.Profile.VisualStyle != RangedVisualStyle.Ballistic)
                {
                    PlaySurfaceHitSound(impactPoint, weapon.Profile.VisualStyle, true);
                }
                landed = true;
            }
            else if (resolved && hitStructural)
            {
                float force = weapon.Profile.ResolveStructuralForce();
                if (force > 0f
                    && SubSystems.TryGet(out StructuralDamageSubSystem structural)
                    && structural.TryApplyStructuralDamage(structuralCoord, force, StructuralDamageSource.Ranged,
                        impactPoint, impactNormal))
                {
                    landed = true;
                }
            }

            if (hasImpact && !hitLiving)
            {
                // A landed structural hit already plays its material's sound from the impact message.
                if (!landed)
                {
                    PlaySurfaceHitSound(impactPoint, weapon.Profile.VisualStyle);
                }

                if (weapon.Profile.VisualStyle == RangedVisualStyle.Ballistic)
                {
                    ObserversNotifyBulletHole(impactPoint, impactNormal);
                }
            }

            ClearMeleeAimPoint();
            ServerNotifyRangedFireState(weapon, landed, hasImpact, impactPoint, shotDirection,
                hasImpact ? impactPoint : aimRay.origin + shotDirection * maxRange, impactNormal);
        }

        /// <summary>
        /// Server only: what a shot that lands on a living body does to it (damage, stamina, knockdown).
        /// A stun on a zone shielded by <see cref="ArmorProfile.BlocksStun"/> armor only drains part of the stamina.
        /// </summary>
        public static void ServerApplyLivingHit(RangedWeaponProfile profile, HumanHealthController health, BodyZone zone)
        {
            bool stunBlocked = profile.KnockdownSeconds > 0f && health.ServerZoneBlocksStun(zone);
            if (profile.BruteDamage > 0f || profile.BurnDamage > 0f)
            {
                health.ApplyDamage(zone, profile.ToDamagePacket());
            }
            if (profile.StaminaDamage > 0f)
            {
                StaminaController targetStamina = health.GetComponentInParent<StaminaController>();
                if (targetStamina != null)
                {
                    float drain = profile.StaminaDamage * (stunBlocked ? RangedWeaponProfile.StunBlockedStaminaFactor : 1f);
                    targetStamina.ServerDepleteStamina(Mathf.Min(drain, targetStamina.ServerCurrentStaminaAbsolute));
                }
            }
            if (profile.KnockdownSeconds > 0f && !stunBlocked)
            {
                Ragdoll targetRagdoll = health.GetComponentInParent<Ragdoll>();
                if (targetRagdoll != null)
                {
                    targetRagdoll.ServerKnockdownAtLeast(profile.KnockdownSeconds);
                }
            }
        }

        [ServerRpc]
        private void CmdRunRangedReload()
        {
            if (!TryGetHeldRangedWeapon(out _, out RangedWeaponItemExtension weapon))
            {
                _interactionController.RejectInteractionForOwner();
                return;
            }

            if (!weapon.ServerTryBeginReload())
            {
                _interactionController.RejectInteractionForOwner();
                return;
            }

            ServerNotifyRangedReloadStarted(weapon);
        }

        [Server]
        private void PlayGunfireSound(Vector3 position, RangedVisualStyle style)
        {
            string[] clips = CombatAudioTrackIds.FireFor(style);
            string clipId = clips[UnityEngine.Random.Range(0, clips.Length)];
            float pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            SubSystems.Get<AudioSubSystem>()?.PlayAudioSource(
                AudioType.Sfx, clipId, position, null, false, 1f, pitch, 14f, 90f);
        }

        [Server]
        private void PlayGunEmptySound(RangedWeaponItemExtension weapon)
        {
            Vector3 position = transform.position;
            if (weapon != null)
            {
                weapon.GetMuzzleWorldPose(out position, out _);
            }

            SubSystems.Get<AudioSubSystem>()?.PlayAudioSource(
                AudioType.Sfx, weapon != null && weapon.Profile.VisualStyle != RangedVisualStyle.Ballistic
                    ? CombatAudioTrackIds.EnergyReady : CombatAudioTrackIds.GunEmpty, position, null, false, 1f, 1f, 8f, 40f);
        }

        [Server]
        private static void PlaySurfaceHitSound(Vector3 impactPoint, RangedVisualStyle style, bool living = false)
        {
            string[] clips = style == RangedVisualStyle.Ballistic
                ? CombatAudioTrackIds.SurfaceHit
                : living ? CombatAudioTrackIds.EnergyFleshHit : CombatAudioTrackIds.EnergyHit;
            string clipId = clips[UnityEngine.Random.Range(0, clips.Length)];
            float pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            SubSystems.Get<AudioSubSystem>()?.PlayAudioSource(
                AudioType.Sfx, clipId, impactPoint, null, false, 0.95f, pitch, 6f, 50f);
        }

        [Server]
        public void ServerNotifyRangedReloadStarted(RangedWeaponItemExtension weapon)
        {
            if (Owner == null || weapon == null)
            {
                return;
            }

            SubSystems.Get<AudioSubSystem>()?.PlayAudioSource(
                AudioType.Sfx, weapon.Profile.VisualStyle == RangedVisualStyle.Ballistic
                    ? CombatAudioTrackIds.ReloadMagazineOut : CombatAudioTrackIds.EnergyReady, transform.position, null, false, 1f, 1f, 8f, 40f);

            TargetNotifyRangedReload(
                Owner,
                weapon.Profile.ReloadSeconds,
                weapon.RoundsRemaining,
                weapon.RecoilStacks);
        }

        [Server]
        private void ServerNotifyRangedFireState(
            RangedWeaponItemExtension weapon,
            bool landed,
            bool hasImpact,
            Vector3 impactPoint,
            Vector3 shotDirection,
            Vector3 endpoint,
            Vector3 impactNormal)
        {
            if (Owner == null || weapon == null)
            {
                return;
            }

            weapon.GetMuzzleWorldPose(out Vector3 muzzlePosition, out Vector3 muzzleForward);
            ObserversNotifyMuzzleFlash(muzzlePosition, muzzleForward, weapon.Profile.VisualStyle,
                endpoint, hasImpact, impactNormal);

            TargetNotifyRangedFireState(
                Owner,
                weapon.Profile.FireCooldownSeconds,
                weapon.RoundsRemaining,
                weapon.RecoilStacks,
                landed,
                hasImpact,
                impactPoint,
                shotDirection);
        }

        [ObserversRpc(RunLocally = true)]
        private void ObserversNotifyMuzzleFlash(Vector3 fallbackPosition, Vector3 fallbackForward,
            RangedVisualStyle style, Vector3 endpoint, bool hasImpact, Vector3 impactNormal)
        {
            style = EnergyWeaponVfx.PreviewStyle ?? style;
            HumanoidIkController ik = GetComponentInChildren<HumanoidIkController>();
            if (ik != null)
            {
                ik.PlayShotKick();
            }

            if (style != RangedVisualStyle.Ballistic)
            {
                Transform socket = TryGetHeldRangedWeapon(out _, out RangedWeaponItemExtension energyWeapon)
                    ? energyWeapon.Muzzle : null;
                EnergyWeaponVfx.Play(style, socket, fallbackPosition, endpoint, hasImpact, impactNormal);
                return;
            }
            if (TryGetHeldRangedWeapon(out _, out RangedWeaponItemExtension weapon))
            {
                Transform muzzle = weapon.Muzzle;
                if (muzzle != null)
                {
                    MuzzleFlashVfx.Play(muzzle);
                    return;
                }

                weapon.GetMuzzleWorldPose(out fallbackPosition, out fallbackForward);
            }

            MuzzleFlashVfx.Play(fallbackPosition, fallbackForward);
        }

        [ObserversRpc(RunLocally = true)]
        private void ObserversNotifyBulletHole(Vector3 impactPoint, Vector3 impactNormal)
        {
            if (EnergyWeaponVfx.PreviewStyle is null or RangedVisualStyle.Ballistic)
            {
                BulletHoleDecalSpawner.Spawn(impactPoint, impactNormal);
            }
        }

        [TargetRpc]
        private void TargetNotifyRangedFireState(
            NetworkConnection connection,
            float cooldownSeconds,
            int rounds,
            float recoilStacks,
            bool landed,
            bool hasImpact,
            Vector3 impactPoint,
            Vector3 shotDirection)
        {
            if (TryGetHeldRangedWeapon(out _, out RangedWeaponItemExtension weapon))
            {
                weapon.BeginLocalFireCooldown();
                weapon.ClientSetRounds(rounds);
                weapon.ClientSetRecoil(recoilStacks);
            }

            if (hasImpact)
            {
                RangedShotFeedback.NotifyLocalShotImpact(impactPoint, landed, shotDirection);
            }
            else if (landed)
            {
                MeleeConnectFeedback.NotifyLocalConnectHitLanded();
            }
        }

        [TargetRpc]
        private void TargetNotifyRangedReload(
            NetworkConnection connection,
            float reloadSeconds,
            int rounds,
            float recoilStacks)
        {
            if (TryGetHeldRangedWeapon(out _, out RangedWeaponItemExtension weapon))
            {
                weapon.BeginLocalReload(reloadSeconds);
                weapon.ClientSetRounds(rounds);
                weapon.ClientSetRecoil(recoilStacks);
            }
        }

        [ServerOrClient]
        private float GetHorizontalMoveSpeed()
        {
            if (TryGetComponent(out CharacterController character) && character != null)
            {
                Vector3 v = character.velocity;
                v.y = 0f;
                return v.magnitude;
            }

            return 0f;
        }

        [Client]
        private static void BeginLocalSwingCycle(Hand hand, MeleeWeaponProfile profile)
        {
            if (hand == null)
            {
                return;
            }

            if (!hand.TryGetComponent(out MeleeRecoveryTracker tracker))
            {
                tracker = hand.gameObject.AddComponent<MeleeRecoveryTracker>();
            }

            tracker.BeginSwingCycle(profile.WindupSeconds, profile.RecoverySeconds);
        }

        [ServerRpc]
        private void CmdRunMeleeSwing()
        {
            if (!TryCreateMeleeHitInteraction(out MeleeHitInteraction hit, out Hand hand))
            {
                _interactionController.RejectInteractionForOwner();
                return;
            }

            IntentType intent = _interactionController.CurrentIntent;
            if (!InteractionPipeline.MatchesIntent(hit, intent))
            {
                Log.Warning(this, "Rejected melee swing — intent {intent} is not Harm-whitelisted",
                    Logs.Generic, intent);
                _interactionController.RejectInteractionForOwner();
                return;
            }

            if (!hit.CanStartSwing(hand))
            {
                _interactionController.RejectInteractionForOwner();
                return;
            }

            float effectiveWindupSeconds = hit.ServerBeginSwing(hand);
            ServerScheduleMeleeConnect(hand, hit.Profile, effectiveWindupSeconds);

            _meleeSwingSerial++;
            RpcExecuteMeleeSwing(_meleeSwingSerial);
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcExecuteMeleeSwing(int swingId)
        {
            if (!IsOwner)
            {
                return;
            }

            if (!TryCreateMeleeHitInteraction(out _, out Hand hand))
            {
                return;
            }

            _interactionController.SetClientActiveDelayed(hand, swingId);
            TrySyncMeleeAimToServer();
        }

        [Server]
        private void ServerScheduleMeleeConnect(Hand hand, MeleeWeaponProfile profile, float windupSeconds)
        {
            _pendingMeleeHand = hand;
            _pendingMeleeProfile = profile;
            _pendingMeleeConnectAt = Time.time + Mathf.Max(0.01f, windupSeconds);
        }

        [Server]
        public void ClearPendingMeleeConnect()
        {
            _pendingMeleeConnectAt = -1f;
            _pendingMeleeHand = null;
            _pendingMeleeProfile = default;
        }

        private void HandleServerMeleeConnectUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (!IsServer || _pendingMeleeConnectAt < 0f || Time.time < _pendingMeleeConnectAt)
            {
                return;
            }

            Hand hand = _pendingMeleeHand;
            MeleeWeaponProfile profile = _pendingMeleeProfile;
            ClearPendingMeleeConnect();

            if (_interactionController.CurrentIntent != IntentType.Harm)
            {
                ClearMeleeAimPoint();
                return;
            }

            if (hand == null || !hand.CanUseHand)
            {
                return;
            }

            var hit = new MeleeHitInteraction(profile);
            hit.ServerApplyConnect(hand, this);
        }

        [ServerOrClient]
        private bool TryCreateMeleeHitInteraction(out MeleeHitInteraction hit, out Hand hand)
        {
            hit = null;
            hand = null;

            IInteractionSource source = InteractionDiscovery.GetActiveInteractionSource(_interactionController);
            if (source == null)
            {
                return false;
            }

            hand = ResolveSwingHand(source);
            if (hand == null)
            {
                return false;
            }

            if (source is Item item)
            {
                item.Source = hand;
            }

            MeleeWeaponProfile profile = ResolveMeleeProfile(source);
            hit = new MeleeHitInteraction(profile);
            return true;
        }

        [ServerOrClient]
        private static Hand ResolveSwingHand(IInteractionSource source)
        {
            if (source == null)
            {
                return null;
            }

            if (source.GetRootSource() is Hand rootHand)
            {
                return rootHand;
            }

            return source.GetComponentInTree<Hand>();
        }

        [ServerOrClient]
        private static MeleeWeaponProfile ResolveMeleeProfile(IInteractionSource source)
        {
            if (source is Item item)
            {
                if (item.TryGetComponent(out MeleeWeaponItemExtension dedicated))
                {
                    return dedicated.Profile;
                }

                return MeleeWeaponProfile.Improvised;
            }

            return MeleeWeaponProfile.Fists;
        }

        private void TryPlayMeleeSwingTelegraph(IInteraction interaction, Hand hand)
        {
            if (interaction is not MeleeHitInteraction hit)
            {
                return;
            }

            if (!TryGetComponent(out HumanoidCombatController combat))
            {
                return;
            }

            // Same exertion scaling as ServerBeginSwing (ExertionPenalty is a SyncVar), so the
            // strike frame lands when the server connects.
            StaminaController stamina = hand != null ? hand.GetComponentInParent<StaminaController>() : null;
            float windup = hit.Profile.WindupSeconds
                * MeleeHitInteraction.ComputeExertionTimeMultiplier(stamina != null ? stamina.ExertionPenalty : 0f);
            combat.RequestAttack(AnimationTriggerId.AttackSwing, windup);
        }

        private void TryPlayRangedFireTelegraph()
        {
            if (!TryGetComponent(out HumanoidCombatController combat))
            {
                return;
            }

            combat.RequestAttack(AnimationTriggerId.FireRifle);
        }

        private void TryPlayRangedReloadTelegraph()
        {
            if (!TryGetComponent(out HumanoidCombatController combat))
            {
                return;
            }

            combat.RequestAttack(AnimationTriggerId.Reload);
        }

        /// <summary>
        /// Client-synced camera aim ray for the active melee swing (matches zone reticle; not hand bone).
        /// </summary>
        [Server]
        public bool TryGetMeleeAimRay(out Ray aimRay)
        {
            aimRay = default;
            if (!_hasMeleeAimRay)
            {
                return false;
            }

            Vector3 direction = _meleeAimPoint - _meleeAimRayOrigin;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            aimRay = new Ray(_meleeAimRayOrigin, direction.normalized);
            return true;
        }

        [Server]
        public void ClearMeleeAimPoint()
        {
            _hasMeleeAimRay = false;
            _meleeAimRayOrigin = default;
            _meleeAimPoint = default;
        }

        private void TrySyncMeleeAimDuringSwing()
        {
            if (_interactionController.CurrentIntent != IntentType.Harm)
            {
                return;
            }

            Hands hands = GetComponent<Hands>();
            Hand hand = hands != null ? hands.SelectedHand : null;
            if (hand == null
                || !hand.TryGetComponent(out MeleeRecoveryTracker tracker)
                || !tracker.IsBusy)
            {
                return;
            }

            TrySyncMeleeAimToServer();
        }

        private void TrySyncMeleeAimToServer()
        {
            if (!IsOwner)
            {
                return;
            }

            if (!TryGetComponent(out HumanoidController humanoid))
            {
                return;
            }

            if (!humanoid.TryGetCombatAimRay(out Ray aimRay, out Vector3 aimPoint))
            {
                return;
            }

            CmdSyncMeleeAim(aimRay.origin, aimPoint);
        }

        [ServerRpc(RequireOwnership = true)]
        private void CmdSyncMeleeAim(Vector3 rayOrigin, Vector3 aimPoint)
        {
            _meleeAimRayOrigin = rayOrigin;
            _meleeAimPoint = aimPoint;
            _hasMeleeAimRay = true;
        }

        /// <summary>
        /// Server → owning client: melee swing cycle lock started (windup+recovery).
        /// </summary>
        [Server]
        public void ServerNotifyMeleeRecovery(Hand hand, float cycleSeconds)
        {
            if (Owner == null || cycleSeconds <= 0f)
            {
                return;
            }

            int handIndex = -1;
            if (hand != null && hand.HandsController is Hands hands)
            {
                handIndex = hands.PlayerHands.IndexOf(hand);
            }

            TargetNotifyMeleeRecovery(Owner, handIndex, cycleSeconds);
        }

        [TargetRpc]
        private void TargetNotifyMeleeRecovery(NetworkConnection connection, int handIndex, float cycleSeconds)
        {
            Hand hand = ResolveLocalHand(handIndex);
            if (hand != null)
            {
                if (!hand.TryGetComponent(out MeleeRecoveryTracker tracker))
                {
                    tracker = hand.gameObject.AddComponent<MeleeRecoveryTracker>();
                }

                tracker.BeginSwingCycle(0f, cycleSeconds);
            }

            MeleeRecoveryFeedback.NotifyLocalRecoveryStarted(cycleSeconds);
        }

        /// <summary>
        /// Server → owning client: melee connect applied damage.
        /// </summary>
        [Server]
        public void ServerNotifyMeleeConnectHit()
        {
            if (Owner == null)
            {
                return;
            }

            TargetNotifyMeleeConnectHit(Owner);
        }

        [TargetRpc]
        private void TargetNotifyMeleeConnectHit(NetworkConnection connection)
        {
            MeleeConnectFeedback.NotifyLocalConnectHitLanded();
        }

        private Hand ResolveLocalHand(int handIndex)
        {
            Hands hands = GetComponentInChildren<Hands>();
            if (hands == null)
            {
                return null;
            }

            if (handIndex >= 0 && handIndex < hands.PlayerHands.Count)
            {
                return hands.PlayerHands[handIndex];
            }

            return hands.SelectedHand;
        }
    }
}
