using FishNet.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Examine;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Combat;
using SS3D.Systems.Screens;
using SS3D.Systems.Selection;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Interactions;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Player-owned interaction router: input policy (Shift Search → armed → Harm combat → Help primary),
    /// intent SyncVar, world/inventory/examine RPCs, and delayed-interaction tracking.
    /// Combat Harm primary lives on sibling <see cref="CombatInteractionNetwork"/>.
    /// Discovery/dispatch/outline helpers: <see cref="InteractionDiscovery"/>,
    /// <see cref="InteractionDispatch"/>, <see cref="InteractionOutlineDriver"/>.
    /// </summary>
    public sealed partial class InteractionController : NetworkActor, IIntentProvider
    {
        private Controls.InteractionsActions _controls;
        private Controls.HotkeysActions _hotkeysControls;
        private InputAction _cancelInteractionAction;
        private InputSubSystem _inputSystem;
        private IInputHandle _gameplayHandle;

        private Camera _camera;
        private RadialInteractionSubSystem _radialView;
        private ArmedInteractionSubSystem _armedSystem;
        private SelectionSubSystem _selectionSystem;

        [SyncVar(OnChange = nameof(SyncIntent))] private IntentType _currentIntent = IntentType.Help;

        private IntentType _ownerIntent = IntentType.Help;

        private CombatInteractionNetwork _combatNetwork;

        private bool _armedEvaluatorSubscribed;

        private readonly InteractionOutlineDriver _outlineDriver = new();
        private readonly DelayedInteractionTracker _delayedTracker = new();

        public IntentType CurrentIntent => IsOwner ? _ownerIntent : _currentIntent;

        /// <summary>
        /// Fired on the owning client when a character-examine take windup is accepted by the server.
        /// </summary>
        public event Action<CharacterExamineSlot, float> TakeFromCharacterStarted;

        /// <summary>
        /// Fired on the owning client when a character-examine take ends (complete, cancel, or reject).
        /// </summary>
        public event Action TakeFromCharacterEnded;

        /// <summary>
        /// True while the owning client is tracking an active delayed interaction (including take-from-character).
        /// </summary>
        public bool HasActiveDelayedInteraction => IsClient && _delayedTracker.HasClientActive;

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            if (IsOwner)
            {
                SubscribeToInput();
            }
            else if (prevOwner.Equals(LocalConnection))
            {
                UnsubscribeFromInput();
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            // Wire inputs before camera — a missing PlayerCamera must not leave _controls null
            // (OnOwnershipClient → SubscribeToInput would cascade-NRE).
            _inputSystem = SubSystems.Get<InputSubSystem>();
            Controls controls = _inputSystem.Inputs;
            _controls = controls.Interactions;
            _hotkeysControls = controls.Hotkeys;
            _cancelInteractionAction = controls.Interactions.Get().FindAction("Cancel Interaction", throwIfNotFound: true);

            _radialView = SubSystems.Get<RadialInteractionSubSystem>();
            _armedSystem = SubSystems.Get<ArmedInteractionSubSystem>();
            _selectionSystem = SubSystems.Get<SelectionSubSystem>();

            Actor playerCamera = SubSystems.Get<CameraSubSystem>()?.PlayerCamera;
            if (playerCamera != null)
            {
                _camera = playerCamera.GetComponent<Camera>();
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                Log.Error(this, "No gameplay camera resolved for InteractionController", Logs.Important);
            }

            _combatNetwork = GetComponent<CombatInteractionNetwork>();
        }

        private void Update()
        {
            if (IsServer)
            {
                RefreshServerDelayedInteractionTracking();
                TickPendingHandAction();
            }

            if (!IsOwner)
            {
                return;
            }

            // Client-side tracking for interactions that registered a ClientInteract instance.
            // Take-from-character (CreateClient => null) waits for TargetNotify / Cancel instead.
            RefreshClientDelayedInteractionTracking();
        }

        private void LateUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            _outlineDriver.Refresh(_selectionSystem, _camera, CurrentIntent, GetActiveInteractionSource());
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            if (IsOwner)
            {
                SubscribeToInput();
            }
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            if (IsOwner)
            {
                _outlineDriver.Clear();
            }

            // Not gated on IsOwner: when the session ends, ownership is already gone by the time the body is
            // torn down, but the input actions outlive it and would keep calling into the destroyed controller.
            UnsubscribeFromInput();
        }

        private void SubscribeToInput()
        {
            if (!_armedEvaluatorSubscribed && _armedSystem != null)
            {
                _armedSystem.EvaluateTarget += EvaluateArmedTarget;
                _armedEvaluatorSubscribed = true;
            }

            // OnEnabled and OnOwnershipClient can both fire for an owner; subscribe exactly once.
            if (_gameplayHandle != null)
            {
                return;
            }

            _controls.RunPrimary.performed += HandleRunPrimary;
            _controls.ViewInteractions.performed += HandleView;
            _cancelInteractionAction.performed += HandleCancelInteraction;
            _hotkeysControls.Use.performed += HandleUse;
            _gameplayHandle = _inputSystem.PushContext(InputContext.Gameplay);
        }

        private void UnsubscribeFromInput()
        {
            if (_armedEvaluatorSubscribed)
            {
                if (_armedSystem != null)
                {
                    _armedSystem.EvaluateTarget -= EvaluateArmedTarget;
                }

                _armedEvaluatorSubscribed = false;
            }

            _controls.RunPrimary.performed -= HandleRunPrimary;
            _controls.ViewInteractions.performed -= HandleView;
            _cancelInteractionAction.performed -= HandleCancelInteraction;
            _hotkeysControls.Use.performed -= HandleUse;
            _gameplayHandle?.Dispose();
            _gameplayHandle = null;
        }

        [Client]
        private void HandleCancelInteraction(InputAction.CallbackContext callbackContext)
        {
            if (!_delayedTracker.HasClientActive)
            {
                return;
            }

            int referenceId = _delayedTracker.ClientActiveReferenceId;
            ClearClientActiveInteractionTracking();
            InteractionOptimisticFeedback.Clear(transform);
            InteractionOutlineView.ClearPending();
            TakeFromCharacterEnded?.Invoke();
            CmdCancelInteraction(referenceId);
        }

        /// <summary>
        /// Runs the most prioritised interaction
        /// </summary>
        [Client]
        public void HandleRunPrimary(InputAction.CallbackContext callbackContext)
        {
            // Caps Lock is sprint; Shift is examine-only (2026-07 input scheme).
            // Read Shift from Keyboard — DetailedExamine.IsPressed can lag behind the device.
            if (IsShiftModifierHeld() && TryRunSearchOnCharacterSelection())
            {
                return;
            }

            if (InputInterface.IsPointerOverInterface())
            {
                return;
            }

            // Throw mode (R) replaces this click's interaction, in Help and Harm alike.
            if (GetCombatNetwork()?.TryRunThrowPrimary() == true)
            {
                return;
            }

            if (_armedSystem.IsArmed)
            {
                TryResolveArmedInteraction();
                return;
            }

            // Harm is combat-exclusive: ranged fire when holding a firearm, else melee swing.
            // Never fall through to Drop/Open/MI.
            if (CurrentIntent == IntentType.Harm)
            {
                CombatInteractionNetwork combat = GetCombatNetwork();
                if (combat != null && combat.TryRunRangedFirePrimary())
                {
                    return;
                }

                combat?.TryRunMeleeSwingPrimary();
                return;
            }

            List<InteractionEntry> viableInteractions = FilterRadialInteractions(
                GetViableInteractionsFromSelection(out InteractionEvent interactionEvent));

            if (viableInteractions.Count <= 0)
            {
                return;
            }

            InteractionEntry interaction = viableInteractions[0];
            interactionEvent.Target = interaction.Target ?? ResolveFallbackTarget(interactionEvent, interaction);

            Log.Information(this, "Running interaction {interactionId} on target {target}", Logs.Generic, interaction.Id.GenericName, interaction.Target);
            if (!TryGetNetworkTargetForDispatch(interaction, interactionEvent, out NetworkObject networkTarget))
            {
                return;
            }

            if (!BeginOwnerInteraction(interaction.Interaction, interactionEvent))
            {
                return;
            }

            InteractionOptimisticFeedback.TryBeginDelayed(interaction.Interaction, interactionEvent);
            InteractionOutlineView.TryBeginPending(interaction.Interaction, interactionEvent);
            CmdRunInteraction(networkTarget, interactionEvent.Point, interaction.Id.GenericName, interaction.Id.TargetComponentIndex);
        }

        /// <summary>
        /// Shift+Click shortcut for <see cref="SearchInteraction"/> — same Discover/RPC path as the radial petal.
        /// </summary>
        [Client]
        private bool TryRunSearchOnCharacterSelection()
        {
            if (_selectionSystem == null)
            {
                SubSystems.TryGet(out _selectionSystem);
            }

            if (!CharacterExamineTargetUtility.TryResolveFromSelectable(
                    _selectionSystem?.GetCurrentSelectable(),
                    out _,
                    out HumanInventory victimInventory))
            {
                return false;
            }

            HumanInventory selfInventory = GetComponent<HumanInventory>();
            if (selfInventory == null)
            {
                selfInventory = GetComponentInChildren<HumanInventory>();
            }

            if (!CharacterLootUtility.IsOtherCharacter(selfInventory, victimInventory))
            {
                return false;
            }

            List<InteractionEntry> viableInteractions = FilterRadialInteractions(
                GetViableInteractionsFromSelection(out InteractionEvent interactionEvent));

            InteractionEntry searchEntry = default;
            bool found = false;
            for (int i = 0; i < viableInteractions.Count; i++)
            {
                if (viableInteractions[i].Interaction?.GetGenericName() == SearchInteraction.GenericName)
                {
                    searchEntry = viableInteractions[i];
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            interactionEvent.Target = searchEntry.Target ?? ResolveFallbackTarget(interactionEvent, searchEntry);
            if (!TryGetNetworkTargetForDispatch(searchEntry, interactionEvent, out NetworkObject networkTarget))
            {
                return false;
            }

            if (!BeginOwnerInteraction(searchEntry.Interaction, interactionEvent))
            {
                return false;
            }

            InteractionOptimisticFeedback.TryBeginDelayed(searchEntry.Interaction, interactionEvent);
            InteractionOutlineView.TryBeginPending(searchEntry.Interaction, interactionEvent);
            CmdRunInteraction(networkTarget, interactionEvent.Point, searchEntry.Id.GenericName, searchEntry.Id.TargetComponentIndex);
            return true;
        }

        private bool IsShiftModifierHeld()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null
                && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            {
                return true;
            }

            // Caps Lock is sprint; Shift is examine-only after the 2026-07 input scheme pass.
            return _inputSystem != null && _inputSystem.DetailedExamine.IsPressed();
        }

        /// <summary>
        /// Starts a delayed take of the item in <paramref name="slot"/> from <paramref name="victim"/>
        /// into the active hand. Paperdoll slots are not Discover targets — this bypasses
        /// <see cref="CmdRunInteraction"/> name resolution.
        /// </summary>
        [Client]
        public void RequestTakeFromCharacter(HumanInventory victim, CharacterExamineSlot slot)
        {
            if (!IsOwner || victim == null || victim.NetworkObject == null)
            {
                return;
            }

            CmdStartTakeFromCharacter(victim.NetworkObject, (byte)slot);
        }

        /// <summary>
        /// Cancels the active delayed interaction (take windup, CPR, etc.) — same path as the Cancel key.
        /// </summary>
        [Client]
        public void CancelActiveDelayedInteraction()
        {
            if (!_delayedTracker.HasClientActive)
            {
                return;
            }

            int referenceId = _delayedTracker.ClientActiveReferenceId;
            ClearClientActiveInteractionTracking();
            InteractionOptimisticFeedback.Clear(transform);
            InteractionOutlineView.ClearPending();
            TakeFromCharacterEnded?.Invoke();
            CmdCancelInteraction(referenceId);
        }

        [ServerRpc(RequireOwnership = true)]
        private void CmdStartTakeFromCharacter(NetworkObject victimObject, byte slotByte)
        {
            if (victimObject == null
                || !Enum.IsDefined(typeof(CharacterExamineSlot), (int)slotByte))
            {
                TargetRejectTakeFromCharacter(Owner);
                return;
            }

            CharacterExamineSlot slot = (CharacterExamineSlot)slotByte;
            Hands hands = GetComponent<Hands>();
            Hand hand = hands?.SelectedHand;
            if (hand == null)
            {
                TargetRejectTakeFromCharacter(Owner);
                return;
            }

            HumanInventory victimInventory = victimObject.GetComponent<HumanInventory>();
            if (victimInventory == null)
            {
                victimInventory = victimObject.GetComponentInChildren<HumanInventory>();
            }

            HumanInventory takerInventory = GetComponent<HumanInventory>();
            if (takerInventory == null)
            {
                takerInventory = GetComponentInChildren<HumanInventory>();
            }

            if (victimInventory == null
                || !CharacterLootUtility.IsOtherCharacter(takerInventory, victimInventory)
                || !CharacterLootUtility.IsLootable(victimInventory)
                || !CharacterExamineContentBuilder.TryGetItemInSlot(victimInventory, slot, out Item item))
            {
                TargetRejectTakeFromCharacter(Owner);
                return;
            }

            Vector3 point = victimInventory.transform.position;
            InteractionEvent interactionEvent = new(hand, item, point, Vector3.up);
            TakeFromCharacterInteraction interaction = new(victimInventory, slot, item);

            if (!interaction.CanInteract(interactionEvent) || !hand.CanExecuteInteraction(interaction))
            {
                TargetRejectTakeFromCharacter(Owner);
                return;
            }

            InteractionReference reference = hand.Interact(interactionEvent, interaction);
            TrackActiveInteraction(hand, reference, interaction);
            TargetTakeFromCharacterStarted(Owner, (byte)slot, interaction.DelaySeconds, reference.Id);
        }

        [TargetRpc]
        private void TargetTakeFromCharacterStarted(
            NetworkConnection connection,
            byte slotByte,
            float delaySeconds,
            int referenceId)
        {
            Hands hands = GetComponent<Hands>();
            Hand hand = hands?.SelectedHand;
            if (hand == null)
            {
                return;
            }

            _delayedTracker.SetClientActive(hand, referenceId);
            TakeFromCharacterStarted?.Invoke((CharacterExamineSlot)slotByte, delaySeconds);
        }

        [TargetRpc]
        private void TargetRejectTakeFromCharacter(NetworkConnection connection)
        {
            ClearClientActiveInteractionTracking();
            TakeFromCharacterEnded?.Invoke();
        }

        [Client]
        public void RequestToggleIntent()
        {
            _ownerIntent = _ownerIntent == IntentType.Harm ? IntentType.Help : IntentType.Harm;
            CmdSetIntent(_ownerIntent);
            ApplyCombatModeForIntent(_ownerIntent);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (IsOwner)
            {
                _ownerIntent = _currentIntent;
                ApplyCombatModeForIntent(_ownerIntent);
            }
        }

        private void SyncIntent(IntentType oldValue, IntentType newValue, bool asServer)
        {
            if (IsOwner)
            {
                _ownerIntent = newValue;
                ApplyCombatModeForIntent(newValue);
            }
        }

        /// <summary>
        /// Harm always enters combat stance; Help returns to peaceful. Owner-driven Cmd.
        /// </summary>
        [Client]
        private void ApplyCombatModeForIntent(IntentType intent)
        {
            if (!IsOwner)
            {
                return;
            }

            if (!TryGetComponent(out HumanoidBodyStateMachine body))
            {
                return;
            }

            if (intent == IntentType.Harm)
            {
                HumanoidCombatMode stance = HumanoidCombatMode.Melee;
                if (TryGetComponent(out HumanoidBodyStateBridge bridge))
                {
                    stance = bridge.ResolveCombatStance();
                }

                if (body.CombatMode != stance)
                {
                    body.CmdSetCombatMode(stance);
                }
            }
            else if (body.CombatMode.IsCombat())
            {
                body.CmdSetCombatMode(HumanoidCombatMode.Peaceful);
            }
        }

        [Client]
        private void HandleView(InputAction.CallbackContext callbackContext)
        {
            if (_armedSystem.IsArmed)
            {
                _armedSystem.Cancel();
                return;
            }

            if (InputInterface.IsPointerOverInterface())
            {
                return;
            }
            List<InteractionEntry> viableInteractions = FilterRadialInteractions(
                GetViableInteractionsFromSelection(out InteractionEvent interactionEvent));

            ViewTargetInteractions(viableInteractions, interactionEvent);
        }

        [Client]
        private void HandleUse(InputAction.CallbackContext callbackContext)
        {
            // Activate item in selected hand — reload takes priority for firearms
            // (including two-hand rifles still wielded while the off-hand is selected).
            if (GetCombatNetwork()?.TryRunRangedReloadPrimary() == true)
            {
                return;
            }

            Hands hands = GetComponent<Hands>();
            Item item = hands?.SelectedHand?.ItemInHand;
            if (item != null)
            {
                InteractInHand(item.gameObject, gameObject);
            }
        }

        /// <summary>
        /// Gets and opens the menu for a target's interactions
        /// </summary>
        /// <param name="viableInteractions"></param>
        /// <param name="interactionEvent"></param>
        [Client]
        private void ViewTargetInteractions(List<InteractionEntry> viableInteractions, InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = viableInteractions.Select(entry => entry.Interaction).ToList();

            if (interactions.Count <= 0) { return; }

            _radialView.SuppressLeftButtonForMenu();

            void handleInteractionSelected(IInteraction interaction)
            {
                _radialView.OnInteractionSelected -= handleInteractionSelected;

                if (!TryRouteRadialInteraction(interaction, interactionEvent, out _))
                {
                    return;
                }

                InteractionEntry entry = viableInteractions.Find(e => e.Interaction == interaction);
                if (entry.Interaction == null)
                {
                    return;
                }

                interactionEvent.Target = entry.Target ?? ResolveFallbackTarget(interactionEvent, entry);

                if (!TryGetNetworkTargetForDispatch(entry, interactionEvent, out NetworkObject networkTarget))
                {
                    return;
                }

                if (!BeginOwnerInteraction(entry.Interaction, interactionEvent))
                {
                    return;
                }

                InteractionOptimisticFeedback.TryBeginDelayed(entry.Interaction, interactionEvent);
                InteractionOutlineView.TryBeginPending(entry.Interaction, interactionEvent);
                CmdRunInteraction(networkTarget, interactionEvent.Point, entry.Id.GenericName, entry.Id.TargetComponentIndex);
            }

            _radialView.SetInteractions(interactions, interactionEvent, Mouse.current.position.ReadValue());
            _radialView.OnInteractionSelected += handleInteractionSelected;
            _radialView.ShowInteractionsMenu();
        }

        /// <summary>
        /// Performs an in-hand interaction
        /// </summary>
        /// <param name="target">The target clicked on</param>
        /// <param name="source">The current selected item or the hands</param>
        /// <param name="showMenu">If a selection menu should be shown</param>
        [Client]
        public void InteractInHand(GameObject target, GameObject sourceObject, bool showMenu = false)
        {
            if (!sourceObject.TryGetComponent(out IInteractionSource source))
            {
                return;
            }

            InteractionEvent interactionEvent = new(source, null, source.GameObject.transform.position, Vector3.up);

            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
            List<InteractionEntry> entries = InteractionPipeline.GetViableInteractions(source, targets, interactionEvent, CurrentIntent);

            if (entries.Count < 1)
            {
                return;
            }

            interactionEvent.Target = entries[0].Target;
            List<IInteraction> interactions = entries.Select(entry => entry.Interaction).ToList();

            if (showMenu && interactions.Count > 0)
            {
                Vector3 mousePosition = Mouse.current.position.ReadValue();
                mousePosition.y = Mathf.Max(_radialView.MenuHeight, mousePosition.y);

                _radialView.SetInteractions(interactions, interactionEvent, mousePosition);

                void handleInteractionSelected(IInteraction interaction)
                {
                    _radialView.OnInteractionSelected -= handleInteractionSelected;

                    if (!TryRouteRadialInteraction(interaction, interactionEvent, out _))
                    {
                        return;
                    }

                    InteractionEntry entry = entries.Find(x => x.Interaction == interaction);
                    if (entry.Interaction == null)
                    {
                        return;
                    }

                    if (!BeginOwnerInteraction(entry.Interaction, interactionEvent))
                    {
                        return;
                    }

                    InteractionOptimisticFeedback.TryBeginDelayed(entry.Interaction, interactionEvent);
                    InteractionOutlineView.TryBeginPending(entry.Interaction, interactionEvent);
                    CmdRunInventoryInteraction(target, sourceObject, entry.Id.GenericName, entry.Id.TargetComponentIndex);
                }

                _radialView.OnInteractionSelected += handleInteractionSelected;
            }
            else
            {
                InteractionEntry firstEntry = entries.First();
                if (!BeginOwnerInteraction(firstEntry.Interaction, interactionEvent))
                {
                    return;
                }

                InteractionOptimisticFeedback.TryBeginDelayed(firstEntry.Interaction, interactionEvent);
                InteractionOutlineView.TryBeginPending(firstEntry.Interaction, interactionEvent);
                CmdRunInventoryInteraction(target, sourceObject, firstEntry.Id.GenericName, firstEntry.Id.TargetComponentIndex);
            }
        }

        [ServerRpc]
        private void CmdSetIntent(IntentType intent)
        {
            _currentIntent = intent;
        }

        [Client]
        private bool TryRouteRadialInteraction(IInteraction interaction, InteractionEvent interactionEvent, out string interactionName)
        {
            interactionName = interaction.GetName(interactionEvent);
            InteractionTier tier = interaction.GetInteractionTier(interactionEvent);

            if (tier == InteractionTier.Instant)
            {
                return true;
            }

            _armedSystem.Arm(interaction, interactionEvent, tier, interactionName);
            return false;
        }

        [Client]
        private ArmedTargetEvaluation EvaluateArmedTarget(Selectable selectable)
        {
            if (!_armedSystem.IsArmed)
            {
                return ArmedTargetEvaluation.None;
            }

            ArmedInteractionState state = _armedSystem.CurrentState;
            if (!TryBuildArmedTargetEvent(selectable, state.OriginEvent, out InteractionEvent targetEvent))
            {
                return ArmedTargetEvaluation.None;
            }

            bool isValid = ValidateArmedTarget(state, targetEvent);
            return new ArmedTargetEvaluation(true, isValid);
        }

        [Client]
        private bool TryResolveArmedInteraction()
        {
            ArmedInteractionState state = _armedSystem.CurrentState;
            if (state == null)
            {
                return false;
            }

            if (!_selectionSystem.TryGetCurrentSelectable(out Selectable selectable))
            {
                return false;
            }

            if (!TryBuildArmedTargetEvent(selectable, state.OriginEvent, out InteractionEvent targetEvent))
            {
                return false;
            }

            if (!ValidateArmedTarget(state, targetEvent))
            {
                return false;
            }

            List<InteractionEntry> viableInteractions = GetViableInteractionsFromTarget(
                selectable.gameObject,
                targetEvent.HasPoint,
                targetEvent.Point,
                targetEvent.Normal,
                out _);

            string genericName = state.Interaction.GetGenericName();
            InteractionEntry entry = viableInteractions.Find(e => e.Interaction.GetGenericName() == genericName);
            if (entry.Interaction == null)
            {
                return false;
            }

            targetEvent.Target = entry.Target;

            if (!TryGetNetworkTarget(targetEvent, out NetworkObject networkTarget))
            {
                return false;
            }

            if (!BeginOwnerInteraction(entry.Interaction, targetEvent))
            {
                return false;
            }

            InteractionOptimisticFeedback.TryBeginDelayed(entry.Interaction, targetEvent);
            InteractionOutlineView.TryBeginPending(entry.Interaction, targetEvent);
            _armedSystem.Cancel();
            CmdRunInteraction(networkTarget, targetEvent.Point, entry.Id.GenericName, entry.Id.TargetComponentIndex);
            return true;
        }

        [Client]
        private bool TryBuildArmedTargetEvent(Selectable selectable, InteractionEvent originEvent, out InteractionEvent targetEvent)
        {
            targetEvent = null;

            if (selectable == null || originEvent == null)
            {
                return false;
            }

            IInteractionSource source = originEvent.Source;
            if (!SelectionTargetUtility.TryResolveInteractionPoint(_camera, selectable, out Vector3 point, out Vector3 normal))
            {
                point = selectable.transform.position;
                normal = Vector3.up;
            }

            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, selectable.gameObject);
            if (targets.Count < 1)
            {
                return false;
            }

            targetEvent = new InteractionEvent(source, targets[0], point, normal);
            return true;
        }

        [Client]
        private static bool ValidateArmedTarget(ArmedInteractionState state, InteractionEvent targetEvent)
        {
            if (state.Interaction is ITargetedInteraction targeted)
            {
                return targeted.CanTarget(state.OriginEvent, targetEvent);
            }

            return state.Interaction.CanInteract(targetEvent);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Automation only: picks <paramref name="genericName"/> from the active hand's interactions on
        /// <paramref name="target"/> at <paramref name="point"/> under <paramref name="intent"/>, then sends it
        /// through the same server RPC as a click, so the server revalidates range, intent and gameplay gates.
        /// Returns why it could not be sent, or null.
        /// </summary>
        [Client]
        public string RunInteractionForTesting(GameObject target, Vector3 point, string genericName, IntentType intent)
        {
            if (_ownerIntent != intent)
            {
                RequestToggleIntent();
            }

            List<InteractionEntry> viable = GetViableInteractionsFromTarget(target, true, point, Vector3.up, out InteractionEvent interactionEvent);
            InteractionEntry entry = viable.Find(e => e.Interaction.GetGenericName() == genericName);
            if (entry.Interaction == null)
            {
                return $"No viable '{genericName}' on {target.name}; viable: "
                    + string.Join(", ", viable.Select(e => e.Interaction.GetGenericName()));
            }

            interactionEvent.Target = entry.Target;
            if (!TryGetNetworkTarget(interactionEvent, out NetworkObject networkTarget))
            {
                return $"'{genericName}' target on {target.name} has no NetworkObject";
            }

            if (!BeginOwnerInteraction(entry.Interaction, interactionEvent))
            {
                return $"'{genericName}' dropped: a hand action is still in flight";
            }

            CmdRunInteraction(networkTarget, point, entry.Id.GenericName, entry.Id.TargetComponentIndex);
            return null;
        }
#endif

        /// <summary>
        /// Runs an interaction (chosen on the client) on the server. For reasons of serialization and security, some code is re-run.
        /// </summary>
        [ServerRpc]
        private void CmdRunInteraction(NetworkObject target, Vector3 point, string genericName, int targetComponentIndex)
        {
            if (!TryValidateInteractionTarget(target, out GameObject targetGameObject))
            {
                return;
            }

            List<InteractionEntry> viableInteractions = GetViableInteractionsFromTarget(targetGameObject, point, out InteractionEvent interactionEvent);
            InteractionIdentifier id = new(genericName, targetComponentIndex);

            if (!TryResolveDispatchedInteraction(viableInteractions, id, out InteractionEntry interaction))
            {
                Log.Error(this, "Failed to resolve interaction {genericName} at target index {targetIndex} on {target}",
                    Logs.Generic, genericName, targetComponentIndex, targetGameObject);

                TargetRejectInteraction(Owner);
                return;
            }

            if (!TryValidateGameplayGates(interaction.Interaction, interactionEvent))
            {
                Log.Warning(this, "Rejected interaction {genericName} due to gameplay gates", Logs.Generic, genericName);
                TargetRejectInteraction(Owner);
                return;
            }

            interactionEvent.Target = interaction.Target;

            bool accepted = ServerRunWithAnimation(interaction.Interaction, interactionEvent, () =>
            {
                InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, interaction.Interaction);
                TrackActiveInteraction(interactionEvent.Source, reference, interaction.Interaction);
                RpcExecuteClientInteraction(target, point, genericName, targetComponentIndex, reference.Id);
            });
            if (!accepted)
            {
                TargetRejectInteraction(Owner);
            }
        }

        /// <summary>
        /// Confirms an interaction issued by a client
        /// </summary>
        [ObserversRpc(RunLocally = true)]
        private void RpcExecuteClientInteraction(NetworkObject target, Vector3 point, string genericName, int targetComponentIndex, int referenceId)
        {
            if (!IsOwner)
            {
                return;
            }

            try
            {
                if (!TryValidateInteractionTarget(target, out GameObject targetGameObject))
                {
                    return;
                }

                List<InteractionEntry> viableInteractions = GetViableInteractionsFromTarget(targetGameObject, point, out InteractionEvent interactionEvent);
                InteractionIdentifier id = new(genericName, targetComponentIndex);

                if (!TryResolveDispatchedInteraction(viableInteractions, id, out InteractionEntry interaction))
                {
                    Log.Warning(this, "Observer failed to resolve interaction {genericName} at target index {targetIndex}",
                        Logs.Generic, genericName, targetComponentIndex);

                    return;
                }

                interactionEvent.Target = interaction.Target;
                interactionEvent.Source.ClientInteract(interactionEvent, interaction.Interaction, new InteractionReference(referenceId));
                _delayedTracker.SetClientActive(interactionEvent.Source, referenceId);
            }
            finally
            {
                InteractionOutlineView.ClearPending();
            }
        }

        /// <summary>
        /// Gets all possible interactions from the shader selection pick on the client.
        /// </summary>
        [Client]
        private List<InteractionEntry> GetViableInteractionsFromSelection(out InteractionEvent interactionEvent)
        {
            return InteractionDiscovery.GetViableInteractionsFromSelection(
                GetActiveInteractionSource(),
                _selectionSystem.GetCurrentSelectable(),
                _camera,
                CurrentIntent,
                out interactionEvent);
        }

        /// <summary>
        /// Gets all possible interactions for a resolved target object and interaction point.
        /// </summary>
        [ServerOrClient]
        private List<InteractionEntry> GetViableInteractionsFromTarget(
            GameObject targetGameObject,
            bool hasPoint,
            Vector3 point,
            Vector3 normal,
            out InteractionEvent interactionEvent)
        {
            return InteractionDiscovery.GetViableInteractionsFromTarget(
                GetActiveInteractionSource(),
                targetGameObject,
                hasPoint,
                point,
                normal,
                CurrentIntent,
                out interactionEvent);
        }

        /// <summary>
        /// Resolves a client-dispatched interaction. Exact <see cref="InteractionIdentifier"/> match first;
        /// falls back to generic name when the client hovered a child Selectable (body part) but the RPC
        /// revalidates against the parent NetworkObject root (different target-component indices).
        /// </summary>
        private static bool TryResolveDispatchedInteraction(
            List<InteractionEntry> viableInteractions,
            InteractionIdentifier id,
            out InteractionEntry interaction)
        {
            return InteractionDispatch.TryResolveDispatchedInteraction(viableInteractions, id, out interaction);
        }

        /// <summary>
        /// RPC path: point was chosen on the client and sent over the wire (always treated as resolved).
        /// </summary>
        [ServerOrClient]
        private List<InteractionEntry> GetViableInteractionsFromTarget(GameObject targetGameObject, Vector3 point, out InteractionEvent interactionEvent)
        {
            return InteractionDiscovery.GetViableInteractionsFromTarget(
                GetActiveInteractionSource(),
                targetGameObject,
                point,
                CurrentIntent,
                out interactionEvent);
        }

        [Client]
        private static bool TryGetNetworkTarget(InteractionEvent interactionEvent, out NetworkObject networkObject)
        {
            return InteractionDispatch.TryGetNetworkTarget(interactionEvent, out networkObject);
        }

        [Client]
        private bool TryGetNetworkTargetForDispatch(
            InteractionEntry entry,
            InteractionEvent interactionEvent,
            out NetworkObject networkObject)
        {
            return InteractionDispatch.TryGetNetworkTargetForDispatch(
                entry,
                interactionEvent,
                _selectionSystem.GetCurrentSelectable(),
                out networkObject);
        }

        [Client]
        private static bool TryGetNetworkObject(IInteractionTarget target, out NetworkObject networkObject)
        {
            return InteractionDispatch.TryGetNetworkObject(target, out networkObject);
        }

        [Client]
        private IInteractionTarget ResolveFallbackTarget(InteractionEvent interactionEvent, InteractionEntry entry)
        {
            return InteractionDispatch.ResolveFallbackTarget(
                interactionEvent,
                entry,
                _selectionSystem.GetCurrentSelectable(),
                GetActiveInteractionSource());
        }

        [Client]
        private static List<InteractionEntry> FilterRadialInteractions(List<InteractionEntry> interactions)
        {
            return InteractionDispatch.FilterRadialInteractions(interactions);
        }

        [ServerOrClient]
        private static bool TryValidateInteractionTarget(NetworkObject target, out GameObject targetGameObject)
        {
            return InteractionDispatch.TryValidateInteractionTarget(target, out targetGameObject);
        }

        private void ClearInteractionOutline()
        {
            _outlineDriver.Clear();
        }

        /// <summary>
        /// Gets all valid interaction targets from a game object
        /// </summary>
        [ServerOrClient]
        private List<IInteractionTarget> GetTargetsFromGameObject(IInteractionSource source, GameObject targetGameObject)
        {
            return InteractionDiscovery.GetTargetsFromGameObject(source, targetGameObject);
        }

        [ServerOrClient]
        private IInteractionSource GetActiveInteractionSource()
        {
            return InteractionDiscovery.GetActiveInteractionSource(this);
        }

        [ServerRpc]
        private void CmdRunInventoryInteraction(GameObject target, GameObject sourceObject, string genericName, int targetComponentIndex)
        {
            if (!TryValidateInventorySource(sourceObject))
            {
                Log.Error(this, "Rejected inventory interaction from invalid source {source}", Logs.Generic, sourceObject);
                TargetRejectInteraction(Owner);
                return;
            }

            IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
            InteractionEvent interactionEvent = new(source, null, source.GameObject.transform.position, Vector3.up);

            List<InteractionEntry> entries = InteractionPipeline.GetViableInteractions(source, targets, interactionEvent, CurrentIntent);

            InteractionIdentifier id = new(genericName, targetComponentIndex);

            if (!InteractionEntry.TryResolve(entries, id, out InteractionEntry chosenEntry))
            {
                Log.Error(target, "Failed to resolve inventory interaction {genericName} at target index {targetIndex}",
                    Logs.Generic, genericName, targetComponentIndex);

                TargetRejectInteraction(Owner);
                return;
            }

            if (!TryValidateGameplayGates(chosenEntry.Interaction, interactionEvent))
            {
                Log.Warning(this, "Rejected inventory interaction {genericName} due to gameplay gates", Logs.Generic, genericName);
                TargetRejectInteraction(Owner);
                return;
            }

            interactionEvent.Target = chosenEntry.Target;

            bool accepted = ServerRunWithAnimation(chosenEntry.Interaction, interactionEvent, () =>
            {
                InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, chosenEntry.Interaction);
                TrackActiveInteraction(source, reference, chosenEntry.Interaction);
                if (chosenEntry.Interaction is IClientInteractionSource)
                {
                    RpcExecuteClientInventoryInteraction(target, sourceObject, genericName, targetComponentIndex, reference.Id);
                }
            });
            if (!accepted)
            {
                TargetRejectInteraction(Owner);
            }
        }

        /// <summary>
        /// Executes the interaction client-side
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sourceObject"></param>
        /// <param name="interactionName"></param>
        /// <param name="referenceId"></param>
        [ObserversRpc(RunLocally = true)]
        private void RpcExecuteClientInventoryInteraction(GameObject target, GameObject sourceObject, string genericName, int targetComponentIndex, int referenceId)
        {
            if (!IsOwner)
            {
                return;
            }

            try
            {
                IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
                List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
                InteractionEvent interactionEvent = new(source, new InteractionTargetGameObject(target));
                List<InteractionEntry> entries = InteractionPipeline.GetViableInteractions(source, targets, interactionEvent, CurrentIntent);
                InteractionIdentifier id = new(genericName, targetComponentIndex);

                if (!InteractionEntry.TryResolve(entries, id, out InteractionEntry chosenInteraction))
                {
                    Log.Warning(this, "Observer failed to resolve inventory interaction {genericName} at target index {targetIndex}",
                        Logs.Generic, genericName, targetComponentIndex);

                    return;
                }

                interactionEvent.Target = chosenInteraction.Target;
                interactionEvent.Source.ClientInteract(interactionEvent, chosenInteraction.Interaction, new InteractionReference(referenceId));
                _delayedTracker.SetClientActive(source, referenceId);
            }
            finally
            {
                InteractionOutlineView.ClearPending();
            }
        }

        [ServerRpc]
        private void CmdCancelInteraction(int referenceId)
        {
            GetCombatNetwork()?.ClearPendingMeleeConnect();

            if (_delayedTracker.TryCancelServer(referenceId))
            {
                ClearActiveInteractionTracking();
            }
        }

        [Server]
        private void TrackActiveInteraction(IInteractionSource source, InteractionReference reference, IInteraction interaction)
        {
            _delayedTracker.TrackServer(source, reference, interaction);
            UpdateInteractionKneel(interaction);
        }

        [Server]
        private void ClearActiveInteractionTracking()
        {
            _delayedTracker.ClearServer();
            SetServerKneel(false);
            CombatInteractionNetwork combat = GetCombatNetwork();
            combat?.ClearPendingMeleeConnect();
            combat?.ClearMeleeAimPoint();
        }

        private void ClearClientActiveInteractionTracking()
        {
            _delayedTracker.ClearClient();
        }

        [Server]
        private void RefreshServerDelayedInteractionTracking()
        {
            if (!_delayedTracker.TryRefreshServerEnded())
            {
                return;
            }

            SetServerKneel(false);

            // Combat connect may still be pending when a DelayedInteraction ends.
            CombatInteractionNetwork combat = GetCombatNetwork();
            combat?.ClearPendingMeleeConnect();
            combat?.ClearMeleeAimPoint();

            if (Owner != null)
            {
                TargetNotifyDelayedInteractionEnded(Owner);
            }
        }

        [Client]
        private void RefreshClientDelayedInteractionTracking()
        {
            _delayedTracker.RefreshClient();
        }

        [TargetRpc]
        private void TargetNotifyDelayedInteractionEnded(NetworkConnection connection)
        {
            ClearClientActiveInteractionTracking();
            TakeFromCharacterEnded?.Invoke();
        }

        /// <summary>Clears optimistic UI when the server rejects an interaction for the owner.</summary>
        [Server]
        public void RejectInteractionForOwner()
        {
            if (Owner != null)
            {
                TargetRejectInteraction(Owner);
            }
        }

        /// <summary>Melee swing Rpc — no client DelayedInteraction; track id for cancel/aim paths.</summary>
        public void SetClientActiveDelayed(IInteractionSource source, int referenceId)
        {
            _delayedTracker.SetClientActive(source, referenceId);
        }

        private CombatInteractionNetwork GetCombatNetwork()
        {
            if (_combatNetwork == null)
            {
                _combatNetwork = GetComponent<CombatInteractionNetwork>();
            }

            return _combatNetwork;
        }

        [TargetRpc]
        private void TargetRejectInteraction(NetworkConnection connection)
        {
            ClearClientActiveInteractionTracking();
            InteractionOptimisticFeedback.Clear(transform);
            InteractionOutlineView.ClearPending();
            TakeFromCharacterEnded?.Invoke();
        }

        private bool TryValidateGameplayGates(IInteraction interaction, InteractionEvent interactionEvent)
        {
            if (!InteractionPipeline.MatchesIntent(interaction, _currentIntent))
            {
                return false;
            }

            return interactionEvent.Source.CanExecuteInteraction(interaction);
        }

        private bool TryValidateInventorySource(GameObject sourceObject)
        {
            if (sourceObject == null)
            {
                return false;
            }

            if (sourceObject == gameObject)
            {
                return true;
            }

            if (sourceObject.transform.IsChildOf(transform))
            {
                return true;
            }

            Hands hands = GetComponent<Hands>();
            Item item = sourceObject.GetComponent<Item>();

            if (hands != null && item != null && hands.SelectedHand?.ItemInHand == item)
            {
                return true;
            }

            return false;
        }
    }
}