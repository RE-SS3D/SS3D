using FishNet.Connection;
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
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Screens;
using SS3D.Systems.Selection;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Attached to the player, initiates interactions.
    /// </summary>
    public sealed class InteractionController : NetworkActor, IIntentProvider
    {
        private const string ExamineInteractionName = "Examine";

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

        private int _clientActiveReferenceId = -1;
        private IInteractionSource _clientActiveSource;
        private InteractionReference _serverActiveReference;
        private IInteractionSource _serverActiveSource;

        private Selectable _activeOutlineSelectable;
        private InteractionOutlineView _activeOutlineView;

        public IntentType CurrentIntent => IsOwner ? _ownerIntent : _currentIntent;

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            if (IsOwner)
            {
                SubscribeToInput();
                _armedSystem.EvaluateTarget += EvaluateArmedTarget;
            }
            else if (prevOwner.Equals(LocalConnection))
            {
                UnsubscribeFromInput();
                _armedSystem.EvaluateTarget -= EvaluateArmedTarget;
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _radialView = SubSystems.Get<RadialInteractionSubSystem>();
            _armedSystem = SubSystems.Get<ArmedInteractionSubSystem>();
            _selectionSystem = SubSystems.Get<SelectionSubSystem>();
            _camera = SubSystems.Get<CameraSubSystem>().PlayerCamera.GetComponent<Camera>();

            _inputSystem = SubSystems.Get<InputSubSystem>();
            Controls controls = _inputSystem.Inputs;
            _controls = controls.Interactions;
            _hotkeysControls = controls.Hotkeys;
            _cancelInteractionAction = controls.Interactions.Get().FindAction("Cancel Interaction", throwIfNotFound: true);
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            RefreshActiveInteractionTracking();
        }

        private void LateUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            RefreshInteractionOutline();
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
                UnsubscribeFromInput();
                ClearInteractionOutline();
                _armedSystem.EvaluateTarget -= EvaluateArmedTarget;
            }
        }

        private void SubscribeToInput()
        {
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
            if (_clientActiveReferenceId < 0)
            {
                return;
            }

            int referenceId = _clientActiveReferenceId;
            ClearClientActiveInteractionTracking();
            InteractionOptimisticFeedback.Clear(transform);
            InteractionOutlineView.ClearPending();
            CmdCancelInteraction(referenceId);
        }

        /// <summary>
        /// Runs the most prioritised interaction
        /// </summary>
        [Client]
        public void HandleRunPrimary(InputAction.CallbackContext callbackContext)
        {
            if (InputInterface.IsPointerOverInterface())
            {
                return;
            }

            // Melee combat preview: LMB (Run Primary) swings instead of world interactions.
            HumanoidCombatController combat = GetComponent<HumanoidCombatController>();
            if (combat != null && combat.TryHandlePrimaryAttack())
            {
                return;
            }

            if (_armedSystem.IsArmed)
            {
                TryResolveArmedInteraction();
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

            InteractionOptimisticFeedback.TryBeginDelayed(interaction.Interaction, interactionEvent);
            InteractionOutlineView.TryBeginPending(interaction.Interaction, interactionEvent);
            CmdRunInteraction(networkTarget, interactionEvent.Point, interaction.Id.GenericName, interaction.Id.TargetComponentIndex);
        }

        [Client]
        public void RequestToggleIntent()
        {
            _ownerIntent = _ownerIntent == IntentType.Harm ? IntentType.Help : IntentType.Harm;
            CmdSetIntent(_ownerIntent);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (IsOwner)
            {
                _ownerIntent = _currentIntent;
            }
        }

        private void SyncIntent(IntentType oldValue, IntentType newValue, bool asServer)
        {
            if (IsOwner)
            {
                _ownerIntent = newValue;
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
            // Activate item in selected hand
            Hands hands = GetComponent<Hands>();
            if (hands == null)
            {
                return;
            }

            Item item = hands.SelectedHand.ItemInHand;
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

            InteractionEvent interactionEvent = new(source, null, source.GameObject.transform.position);

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

                    InteractionOptimisticFeedback.TryBeginDelayed(entry.Interaction, interactionEvent);
                    InteractionOutlineView.TryBeginPending(entry.Interaction, interactionEvent);
                    CmdRunInventoryInteraction(target, sourceObject, entry.Id.GenericName, entry.Id.TargetComponentIndex);
                }

                _radialView.OnInteractionSelected += handleInteractionSelected;
            }
            else
            {
                InteractionEntry firstEntry = entries.First();
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

            if (!InteractionEntry.TryResolve(viableInteractions, id, out InteractionEntry interaction))
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

            InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, interaction.Interaction);
            TrackActiveInteraction(interactionEvent.Source, reference, interaction.Interaction);
            RpcExecuteClientInteraction(target, point, genericName, targetComponentIndex, reference.Id);
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

                if (!InteractionEntry.TryResolve(viableInteractions, id, out InteractionEntry interaction))
                {
                    Log.Warning(this, "Observer failed to resolve interaction {genericName} at target index {targetIndex}",
                        Logs.Generic, genericName, targetComponentIndex);

                    return;
                }

                interactionEvent.Target = interaction.Target;
                interactionEvent.Source.ClientInteract(interactionEvent, interaction.Interaction, new InteractionReference(referenceId));
                _clientActiveSource = interactionEvent.Source;
                _clientActiveReferenceId = referenceId;
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
            IInteractionSource source = GetActiveInteractionSource();

            if (source == null)
            {
                interactionEvent = null;
                return new List<InteractionEntry>();
            }

            Selectable current = _selectionSystem.GetCurrentSelectable();
            if (current == null)
            {
                interactionEvent = null;
                return new List<InteractionEntry>();
            }

            SelectionTargetUtility.TryResolveInteractionPoint(_camera, current, out Vector3 point, out Vector3 normal);
            return GetViableInteractionsFromTarget(current.gameObject, point, normal, out interactionEvent);
        }

        /// <summary>
        /// Gets all possible interactions for a resolved target object and interaction point.
        /// </summary>
        [ServerOrClient]
        private List<InteractionEntry> GetViableInteractionsFromTarget(GameObject targetGameObject, Vector3 point, Vector3 normal, out InteractionEvent interactionEvent)
        {
            IInteractionSource source = GetActiveInteractionSource();

            if (source == null)
            {
                interactionEvent = null;
                return new List<InteractionEntry>();
            }

            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, targetGameObject);
            interactionEvent = new InteractionEvent(source, targets[0], point, normal);

            return InteractionPipeline.GetViableInteractions(source, targets, interactionEvent, CurrentIntent);
        }

        [ServerOrClient]
        private List<InteractionEntry> GetViableInteractionsFromTarget(GameObject targetGameObject, Vector3 point, out InteractionEvent interactionEvent)
        {
            return GetViableInteractionsFromTarget(targetGameObject, point, Vector3.zero, out interactionEvent);
        }

        [Client]
        private static bool TryGetNetworkTarget(InteractionEvent interactionEvent, out NetworkObject networkObject)
        {
            networkObject = null;

            if (interactionEvent?.Target == null)
            {
                return false;
            }

            return TryGetNetworkObject(interactionEvent.Target, out networkObject);
        }

        [Client]
        private bool TryGetNetworkTargetForDispatch(
            InteractionEntry entry,
            InteractionEvent interactionEvent,
            out NetworkObject networkObject)
        {
            if (TryGetNetworkTarget(interactionEvent, out networkObject))
            {
                return true;
            }

            if (entry.Target != null)
            {
                return false;
            }

            Selectable current = _selectionSystem.GetCurrentSelectable();
            if (current == null)
            {
                return false;
            }

            networkObject = current.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = current.GetComponentInParent<NetworkObject>();
            }

            return networkObject != null;
        }

        [Client]
        private static bool TryGetNetworkObject(IInteractionTarget target, out NetworkObject networkObject)
        {
            networkObject = null;

            GameObject targetGameObject = null;
            if (target is IGameObjectProvider targetProvider)
            {
                targetGameObject = targetProvider.GameObject;
            }
            else if (target is Component targetComponent)
            {
                targetGameObject = targetComponent.gameObject;
            }

            if (targetGameObject == null)
            {
                return false;
            }

            networkObject = targetGameObject.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = targetGameObject.GetComponentInParent<NetworkObject>();
            }

            return networkObject != null;
        }

        [Client]
        private IInteractionTarget ResolveFallbackTarget(InteractionEvent interactionEvent, InteractionEntry entry)
        {
            if (entry.Target != null)
            {
                return entry.Target;
            }

            if (interactionEvent?.Target != null)
            {
                return interactionEvent.Target;
            }

            Selectable current = _selectionSystem.GetCurrentSelectable();
            if (current == null)
            {
                return null;
            }

            IInteractionSource source = GetActiveInteractionSource();
            if (source == null)
            {
                return null;
            }

            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, current.gameObject);
            return targets.Count > 0 ? targets[0] : null;
        }

        [Client]
        private static List<InteractionEntry> FilterRadialInteractions(List<InteractionEntry> interactions)
        {
            return interactions
                .Where(entry => entry.Interaction.GetGenericName() != ExamineInteractionName)
                .ToList();
        }

        [ServerOrClient]
        private static bool TryValidateInteractionTarget(NetworkObject target, out GameObject targetGameObject)
        {
            targetGameObject = null;

            if (target == null || !target.IsSpawned)
            {
                return false;
            }

            targetGameObject = target.gameObject;

            if (targetGameObject.GetComponent<Selectable>() == null && targetGameObject.GetComponentInChildren<Selectable>() == null)
            {
                return false;
            }

            return true;
        }

        [Client]
        private void RefreshInteractionOutline()
        {
            Selectable current = _selectionSystem.GetCurrentSelectable();
            InteractionOutlineView.ClearPendingExcept(current);

            if (current == null || IsEntityOutlineExcluded(current))
            {
                ClearInteractionOutline();
                return;
            }

            if (InteractionOutlineView.IsPending(current))
            {
                if (current != _activeOutlineSelectable)
                {
                    ClearInteractionOutline();
                    _activeOutlineSelectable = current;
                    _activeOutlineView = InteractionOutlineView.GetOrCreate(current);
                }

                _activeOutlineView?.SetState(InteractionOutlineView.OutlineState.Pending);
                return;
            }

            if (current != _activeOutlineSelectable)
            {
                ClearInteractionOutline();
                _activeOutlineSelectable = current;
                _activeOutlineView = InteractionOutlineView.GetOrCreate(current);
            }

            if (_activeOutlineView == null)
            {
                return;
            }

            if (!TryEvaluateInteractability(current, out bool hasViableInteractions))
            {
                _activeOutlineView.SetState(InteractionOutlineView.OutlineState.Hidden);
                return;
            }

            InteractionOutlineView.OutlineState state = hasViableInteractions
                ? InteractionOutlineView.OutlineState.Available
                : InteractionOutlineView.OutlineState.Unavailable;

            _activeOutlineView.SetState(state);
        }

        /// <summary>
        /// Player-controlled entities use dedicated UIs (e.g. medical) instead of world interaction outlines.
        /// </summary>
        private static bool IsEntityOutlineExcluded(Selectable selectable)
        {
            return selectable.GetComponentInParent<Entity>() != null;
        }

        [Client]
        private bool TryEvaluateInteractability(Selectable selectable, out bool hasViableInteractions)
        {
            hasViableInteractions = false;

            if (GetActiveInteractionSource() == null)
            {
                return false;
            }

            SelectionTargetUtility.TryResolveInteractionPoint(_camera, selectable, out Vector3 point, out Vector3 normal);
            IInteractionSource source = GetActiveInteractionSource();
            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, selectable.gameObject);
            InteractionEvent interactionEvent = new(source, targets.Count > 0 ? targets[0] : null, point, normal);

            List<InteractionEntry> discovered = InteractionPipeline.Discover(source, targets, interactionEvent);
            if (discovered.Count == 0)
            {
                return false;
            }

            List<InteractionEntry> viableInteractions = InteractionPipeline.FilterAndSort(
                source,
                discovered,
                point,
                normal,
                CurrentIntent);

            hasViableInteractions = viableInteractions.Count > 0;
            return true;
        }

        private void ClearInteractionOutline()
        {
            if (_activeOutlineView != null)
            {
                _activeOutlineView.SetState(InteractionOutlineView.OutlineState.Hidden);
            }

            _activeOutlineView = null;
            _activeOutlineSelectable = null;
        }

        /// <summary>
        /// Gets all valid interaction targets from a game object
        /// </summary>
        /// <param name="source">The source of the interaction</param>
        /// <param name="targetGameObject">The game objects the interaction targets are on</param>
        /// <returns>A list of all valid interaction targets</returns>
        [ServerOrClient]
        private List<IInteractionTarget> GetTargetsFromGameObject(IInteractionSource source, GameObject targetGameObject)
        {
            List<IInteractionTarget> targets = new();

            // Get all target components which are not disabled and the source can interact with
            targets.AddRange(targetGameObject.GetComponents<IInteractionTarget>().Where(x => (x as MonoBehaviour)?.enabled != false && source.CanInteractWithTarget(x)));
            if (targets.Count < 1)
            {
                targets.Add(new InteractionTargetGameObject(targetGameObject));
            }

            return targets;
        }

        [ServerOrClient]
        private IInteractionSource GetActiveInteractionSource()
        {
            IHandsController handsController = GetComponent<IHandsController>();
            var interactionSource = handsController.GetActiveInteractionSource();

            return interactionSource;
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
            InteractionEvent interactionEvent = new(source, null, source.GameObject.transform.position);

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

            InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, chosenEntry.Interaction);
            TrackActiveInteraction(source, reference, chosenEntry.Interaction);
            if (chosenEntry.Interaction is IClientInteractionSource)
            {
                RpcExecuteClientInventoryInteraction(target, sourceObject, genericName, targetComponentIndex, reference.Id);
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
                _clientActiveSource = source;
                _clientActiveReferenceId = referenceId;
            }
            finally
            {
                InteractionOutlineView.ClearPending();
            }
        }

        [ServerRpc]
        private void CmdCancelInteraction(int referenceId)
        {
            if (_serverActiveReference == null || _serverActiveReference.Id != referenceId || _serverActiveSource == null)
            {
                return;
            }

            if (!_serverActiveSource.HasInteraction(_serverActiveReference))
            {
                ClearActiveInteractionTracking();
                return;
            }

            _serverActiveSource.CancelInteraction(_serverActiveReference);
            ClearActiveInteractionTracking();
        }

        [Server]
        private void TrackActiveInteraction(IInteractionSource source, InteractionReference reference, IInteraction interaction)
        {
            if (interaction is not IDelayedInteraction)
            {
                return;
            }

            _serverActiveReference = reference;
            _serverActiveSource = source;
        }

        [Server]
        private void ClearActiveInteractionTracking()
        {
            _serverActiveReference = null;
            _serverActiveSource = null;
        }

        private void ClearClientActiveInteractionTracking()
        {
            _clientActiveReferenceId = -1;
            _clientActiveSource = null;
        }

        private void RefreshActiveInteractionTracking()
        {
            if (IsServer && _serverActiveReference != null && _serverActiveSource != null
                && !_serverActiveSource.HasInteraction(_serverActiveReference))
            {
                ClearActiveInteractionTracking();
            }

            if (IsClient && _clientActiveReferenceId >= 0 && _clientActiveSource != null)
            {
                var reference = new InteractionReference(_clientActiveReferenceId);

                if (!_clientActiveSource.HasInteraction(reference))
                {
                    ClearClientActiveInteractionTracking();
                }
            }
        }

        [TargetRpc]
        private void TargetRejectInteraction(NetworkConnection connection)
        {
            ClearClientActiveInteractionTracking();
            InteractionOptimisticFeedback.Clear(transform);
            InteractionOutlineView.ClearPending();
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