using System;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Area;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Interaction animations (animation pass Phase 4). The owner plays the clip at the click; the server picks the
    /// same clip and tells the other peers, with the yaw toward the target. For a clip with a contact frame
    /// (pickup, drop, lockers) the server holds the interaction as the one pending hand action and runs it when the
    /// hand gets there, if it is still allowed then; a knockdown, stun or hand swap before that cancels it. Doors
    /// stay instant. A delayed interaction that kneels (defuse, CPR) sets the replicated kneel bit while it runs.
    /// </summary>
    public sealed partial class InteractionController
    {
        private AnimationOrchestrator _animationOrchestrator;
        private HumanoidIkController _ikController;
        private bool _serverKneeling;

        private float _pendingHandActionAt = -1f;
        private Hand _pendingHandActionHand;
        private Func<bool> _pendingHandActionValid;
        private Action _pendingHandActionRun;

        /// <summary>Owner: Time.time until which a waiting hand action is in flight; further ones are ignored.</summary>
        private float _ownerHandActionUntil = float.NegativeInfinity;

        /// <summary>Server: a pickup, drop or locker is waiting for the hand to make contact.</summary>
        public bool HasPendingHandAction => _pendingHandActionAt >= 0f;

        /// <summary>The clip for <paramref name="interaction"/>, read before it runs (a locker door is as it was).</summary>
        private int ChooseInteractionClip(IInteraction interaction, InteractionEvent interactionEvent, out Vector3 targetPoint)
        {
            targetPoint = ResolveTargetPoint(interaction, interactionEvent);
            bool closesLocker = interaction is LockerDoorInteraction locker && !locker.OpensDoor;
            string name = interaction.GetGenericName();
            if (name == "Toggle" && interactionEvent.Target is LightSwitchController)
            {
                name = InteractionAnimation.LightSwitchName;
            }

            return InteractionAnimation.ChooseClip(name, targetPoint.y - transform.position.y, closesLocker);
        }

        /// <summary>
        /// Owner, before sending an interaction: plays its clip and faces the target. False when a waiting hand
        /// action is still in flight and this one would wait too, so the caller drops the click (revised
        /// Decision 15, Q5: one pending hand action at a time).
        /// </summary>
        [Client]
        private bool BeginOwnerInteraction(IInteraction interaction, InteractionEvent interactionEvent)
        {
            int clip = ChooseInteractionClip(interaction, interactionEvent, out Vector3 targetPoint);
            return BeginOwnerHandAction(clip, targetPoint);
        }

        /// <summary>Owner: <see cref="BeginOwnerInteraction"/> for a clip chosen by the caller (drop hotkey, inventory drag).</summary>
        [Client]
        public bool BeginOwnerHandAction(int clip, Vector3 targetPoint)
        {
            if (clip == InteractionAnimation.None)
            {
                return true;
            }

            float contact = InteractionAnimation.ContactSeconds(clip);
            if (contact > 0f)
            {
                if (Time.time < _ownerHandActionUntil)
                {
                    return false;
                }

                _ownerHandActionUntil = Time.time + contact;
            }

            float yaw = YawToward(targetPoint);
            PlayInteractionLocally(clip, yaw);
            if (TryGetComponent(out HumanoidController controller))
            {
                bool combat = TryGetComponent(out HumanoidBodyStateMachine body) && body.CombatMode.IsCombat();
                if (!controller.HasMovementInput || combat)
                {
                    controller.HoldFacing(yaw, contact + InteractionAnimation.FacingHoldSeconds);
                }
            }

            return true;
        }

        /// <summary>
        /// Server, for an interaction the dispatch has validated: tells the other peers to play its clip, then runs
        /// <paramref name="run"/> now (instant clip) or at the clip's contact frame. False when another hand action
        /// is still pending, so the caller rejects this one.
        /// </summary>
        [Server]
        private bool ServerRunWithAnimation(IInteraction interaction, InteractionEvent interactionEvent, Action run)
        {
            int clip = ChooseInteractionClip(interaction, interactionEvent, out Vector3 targetPoint);
            return ServerRunHandAction(clip, targetPoint,
                () => interaction.CanInteract(interactionEvent) && TryValidateGameplayGates(interaction, interactionEvent),
                run);
        }

        /// <summary>
        /// Server: plays <paramref name="clip"/> on the other peers and runs <paramref name="run"/> at its contact
        /// frame, after <paramref name="stillValid"/> and the body and hand checks pass again (at once for an
        /// instant clip). False when another hand action is pending and this one would wait too.
        /// </summary>
        [Server]
        public bool ServerRunHandAction(int clip, Vector3 targetPoint, Func<bool> stillValid, Action run)
        {
            float contact = InteractionAnimation.ContactSeconds(clip);
            if (contact > 0f && HasPendingHandAction)
            {
                return false;
            }

            if (clip != InteractionAnimation.None)
            {
                RpcPlayInteractionAnimation((byte)clip, YawToward(targetPoint));
            }

            if (contact <= 0f)
            {
                run();
                return true;
            }

            _pendingHandActionAt = Time.time + contact;
            _pendingHandActionHand = GetSelectedHand();
            _pendingHandActionValid = stillValid;
            _pendingHandActionRun = run;
            return true;
        }

        [Server]
        private void ClearPendingHandAction()
        {
            _pendingHandActionAt = -1f;
            _pendingHandActionHand = null;
            _pendingHandActionValid = null;
            _pendingHandActionRun = null;
        }

        /// <summary>Server, every frame: runs the pending hand action at contact unless something interrupted it.</summary>
        [Server]
        private void TickPendingHandAction()
        {
            if (!HasPendingHandAction || Time.time < _pendingHandActionAt)
            {
                return;
            }

            Hand hand = _pendingHandActionHand;
            Func<bool> stillValid = _pendingHandActionValid;
            Action run = _pendingHandActionRun;
            ClearPendingHandAction();

            // As for a throw (combat §2): knocked down, stunned, the hand unusable or swapped, nothing happens.
            bool bodyCanAct = !TryGetComponent(out HumanoidBodyStateMachine body) || body.CanPerformAction();
            bool sameHand = hand == null || (GetSelectedHand() == hand && hand.CanUseHand);
            if (!bodyCanAct || !sameHand || (stillValid != null && !stillValid()))
            {
                if (Owner != null)
                {
                    TargetRejectInteraction(Owner);
                }

                return;
            }

            run?.Invoke();
        }

        private Hand GetSelectedHand() => TryGetComponent(out Hands hands) ? hands.SelectedHand : null;

        private float YawToward(Vector3 targetPoint)
        {
            Vector3 heading = targetPoint - transform.position;
            heading.y = 0f;
            return heading.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(heading).eulerAngles.y
                : transform.eulerAngles.y;
        }

        /// <summary>Server: kneels the body while a kneeling delayed interaction runs.</summary>
        [Server]
        private void UpdateInteractionKneel(IInteraction interaction)
        {
            // Only a delayed interaction replaces the tracked one, so only it changes the kneel.
            if (interaction is IDelayedInteraction)
            {
                SetServerKneel(InteractionAnimation.ChooseClip(interaction.GetGenericName()) == InteractionAnimation.Kneel);
            }
        }

        [Server]
        private void SetServerKneel(bool kneel)
        {
            if (_serverKneeling == kneel)
            {
                return;
            }

            _serverKneeling = kneel;
            if (TryGetComponent(out HumanoidBodyStateMachine body))
            {
                body.SetInteractKneel(kneel);
            }
        }

        /// <summary>Peers other than the owner (who played it at the click): the clip and the torso twist.</summary>
        [ObserversRpc(ExcludeOwner = true)]
        private void RpcPlayInteractionAnimation(byte clip, float yaw) => PlayInteractionLocally(clip, yaw);

        /// <summary>Plays the clip if this body's upper body is free and twists the torso toward the target.</summary>
        private void PlayInteractionLocally(int clip, float yaw)
        {
            if (_animationOrchestrator == null)
            {
                _animationOrchestrator = GetComponent<AnimationOrchestrator>();
            }

            if (_ikController == null)
            {
                _ikController = GetComponentInChildren<HumanoidIkController>();
            }

            _ikController?.SetThrowAimYaw(yaw);
            _animationOrchestrator?.PlayInteraction(clip);
        }

        /// <summary>Where the hand goes: the drop point for a drop, else the target object, else the clicked point.</summary>
        private static Vector3 ResolveTargetPoint(IInteraction interaction, InteractionEvent interactionEvent)
        {
            if (interaction.GetGenericName() != "Drop" && interactionEvent.Target is IGameObjectProvider provider
                && provider.GameObject != null)
            {
                return provider.GameObject.transform.position;
            }

            if (interactionEvent.HasPoint)
            {
                return interactionEvent.Point;
            }

            return interactionEvent.Source?.GameObject != null
                ? interactionEvent.Source.GameObject.transform.position
                : Vector3.zero;
        }
    }
}
