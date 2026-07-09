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
        private Controls.InteractionsActions _controls;
        private Controls.HotkeysActions _hotkeysControls;
        private InputAction _cancelInteractionAction;
        private InputSubSystem _inputSystem;

        private Camera _camera;
        private RadialInteractionSubSystem _radialView;
        private SelectionSubSystem _selectionSystem;

        [SyncVar(OnChange = nameof(SyncIntent))] private IntentType _currentIntent = IntentType.Help;

        private IntentType _ownerIntent = IntentType.Help;

        private int _clientActiveReferenceId = -1;
        private IInteractionSource _clientActiveSource;
        private InteractionReference _serverActiveReference;
        private IInteractionSource _serverActiveSource;

        public IntentType CurrentIntent => IsOwner ? _ownerIntent : _currentIntent;

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

            _radialView = SubSystems.Get<RadialInteractionSubSystem>();
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
            }
        }

        private void SubscribeToInput()
        {
            _controls.RunPrimary.performed += HandleRunPrimary;
            _controls.ViewInteractions.performed += HandleView;
            _cancelInteractionAction.performed += HandleCancelInteraction;
            _hotkeysControls.Use.performed += HandleUse;
            _inputSystem.ToggleActionMap(_controls, true);
        }

        private void UnsubscribeFromInput()
        {
            _controls.RunPrimary.performed -= HandleRunPrimary;
            _controls.ViewInteractions.performed -= HandleView;
            _cancelInteractionAction.performed -= HandleCancelInteraction;
            _hotkeysControls.Use.performed -= HandleUse;
            _inputSystem.ToggleActionMap(_controls, false);
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
            CmdCancelInteraction(referenceId);
        }

        /// <summary>
        /// Runs the most prioritised interaction
        /// </summary>
        [Client]
        public void HandleRunPrimary(InputAction.CallbackContext callbackContext)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            List<InteractionEntry> viableInteractions = GetViableInteractionsFromSelection(out InteractionEvent interactionEvent);

            if (viableInteractions.Count <= 0)
            {
                return;
            }

            InteractionEntry interaction = viableInteractions[0];
            interactionEvent.Target = interaction.Target;

            Log.Information(this, "Running interaction {interactionId} on target {target}", Logs.Generic, interaction.Id.GenericName, interaction.Target);
            if (!TryGetNetworkTarget(interactionEvent, out NetworkObject networkTarget))
            {
                return;
            }

            InteractionOptimisticFeedback.TryBeginDelayed(interaction.Interaction, interactionEvent);
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
            // leftButton is enabled in RadialInteractionView HandleDisappear
            _inputSystem.ToggleBinding("<Mouse>/leftButton", false);
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            List<InteractionEntry> viableInteractions = GetViableInteractionsFromSelection(out InteractionEvent interactionEvent);

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

            void handleInteractionSelected(IInteraction interaction, RadialInteractionButton _)
            {
                _radialView.OnInteractionSelected -= handleInteractionSelected;

                InteractionEntry entry = viableInteractions.Find(e => e.Interaction == interaction);
                if (entry.Interaction == null)
                {
                    return;
                }

                interactionEvent.Target = entry.Target;

                if (!TryGetNetworkTarget(interactionEvent, out NetworkObject networkTarget))
                {
                    return;
                }

                InteractionOptimisticFeedback.TryBeginDelayed(entry.Interaction, interactionEvent);
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
                mousePosition.y = Mathf.Max(_radialView.RectTransform.rect.height, mousePosition.y);

                _radialView.SetInteractions(interactions, interactionEvent, mousePosition);

                void handleInteractionSelected(IInteraction interaction, RadialInteractionButton _)
                {
                    InteractionEntry entry = entries.Find(x => x.Interaction == interaction);
                    if (entry.Interaction == null)
                    {
                        return;
                    }

                    InteractionOptimisticFeedback.TryBeginDelayed(entry.Interaction, interactionEvent);
                    CmdRunInventoryInteraction(target, sourceObject, entry.Id.GenericName, entry.Id.TargetComponentIndex);
                }

                _radialView.OnInteractionSelected += handleInteractionSelected;
            }
            else
            {
                InteractionEntry firstEntry = entries.First();
                InteractionOptimisticFeedback.TryBeginDelayed(firstEntry.Interaction, interactionEvent);
                CmdRunInventoryInteraction(target, sourceObject, firstEntry.Id.GenericName, firstEntry.Id.TargetComponentIndex);
            }
        }

        [ServerRpc]
        private void CmdSetIntent(IntentType intent)
        {
            _currentIntent = intent;
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
        [ObserversRpc]
        private void RpcExecuteClientInteraction(NetworkObject target, Vector3 point, string genericName, int targetComponentIndex, int referenceId)
        {
            if (IsServer)
            {
                return;
            }

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

            GameObject targetGameObject = null;
            if (interactionEvent.Target is IGameObjectProvider targetProvider)
            {
                targetGameObject = targetProvider.GameObject;
            }
            else if (interactionEvent.Target is Component targetComponent)
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
        [ObserversRpc]
        private void RpcExecuteClientInventoryInteraction(GameObject target, GameObject sourceObject, string genericName, int targetComponentIndex, int referenceId)
        {
            if (IsServer)
            {
                return;
            }

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