using FishNet.Connection;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
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
    public sealed class InteractionController : NetworkActor
    {
        private Controls.InteractionsActions _controls;
        private Controls.HotkeysActions _hotkeysControls;
        private InputSubSystem _inputSystem;

        private Camera _camera;
        private RadialInteractionSubSystem _radialView;
        private SelectionSubSystem _selectionSystem;

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
            _hotkeysControls.Use.performed += HandleUse;
            _inputSystem.ToggleActionMap(_controls, true);
        }

        private void UnsubscribeFromInput()
        {
            _controls.RunPrimary.performed -= HandleRunPrimary;
            _controls.ViewInteractions.performed -= HandleView;
            _hotkeysControls.Use.performed -= HandleUse;
            _inputSystem.ToggleActionMap(_controls, false);
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
            string interactionName = interaction.Interaction.GetName(interactionEvent);
            interactionEvent.Target = interaction.Target;

            Log.Information(this, "Running interaction {interactionName} on target {target}", Logs.Generic, interactionName, interaction.Target);
            if (!TryGetNetworkTarget(interactionEvent, out NetworkObject networkTarget))
            {
                return;
            }

            CmdRunInteraction(networkTarget, interactionEvent.Point, interactionName);
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

            void handleInteractionSelected(IInteraction interaction)
            {
                _radialView.OnInteractionSelected -= handleInteractionSelected;
                string interactionName = interaction.GetName(interactionEvent);

                if (!TryGetNetworkTarget(interactionEvent, out NetworkObject networkTarget))
                {
                    return;
                }

                CmdRunInteraction(networkTarget, interactionEvent.Point, interactionName);
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
            List<InteractionEntry> entries = GetInteractionsFromTargets(source, targets, interactionEvent);

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
                    int index = entries.FindIndex(x => x.Interaction == interaction);
                    string interactionName = interaction.GetName(interactionEvent);

                    CmdRunInventoryInteraction(target, sourceObject, index, interactionName);
                }

                _radialView.OnInteractionSelected += handleInteractionSelected;
            }
            else
            {
                IInteraction firstInteraction = entries.First().Interaction;
                CmdRunInventoryInteraction(target, sourceObject, 0, firstInteraction.GetName(interactionEvent));
            }
        }

        /// <summary>
        /// Runs an interaction (chosen on the client) on the server. For reasons of serialization and security, some code is re-run.
        /// </summary>
        [ServerRpc]
        private void CmdRunInteraction(NetworkObject target, Vector3 point, string interactionName)
        {
            if (!TryValidateInteractionTarget(target, out GameObject targetGameObject))
            {
                return;
            }

            List<InteractionEntry> viableInteractions = GetViableInteractionsFromTarget(targetGameObject, point, out InteractionEvent interactionEvent);
            InteractionEntry interaction = viableInteractions.Find(entry => entry.Interaction.GetName(interactionEvent) == interactionName);

            if (interaction.Interaction == null)
            {
                return;
            }

            interactionEvent.Target = interaction.Target;

            InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, interaction.Interaction);
            RpcExecuteClientInteraction(target, point, interactionName, reference.Id);

            // TODO: Keep track of interactions for cancellation
        }

        /// <summary>
        /// Confirms an interaction issued by a client
        /// </summary>
        [ObserversRpc]
        private void RpcExecuteClientInteraction(NetworkObject target, Vector3 point, string interactionName, int referenceId)
        {
            if (!TryValidateInteractionTarget(target, out GameObject targetGameObject))
            {
                return;
            }

            List<InteractionEntry> viableInteractions = GetViableInteractionsFromTarget(targetGameObject, point, out InteractionEvent interactionEvent);
            InteractionEntry interaction =
                viableInteractions.Find(entry => entry.Interaction.GetName(interactionEvent) == interactionName);

            if (interaction.Interaction == null)
            {
                return;
            }

            interactionEvent.Target = interaction.Target;

            if (interaction.Interaction.GetName(interactionEvent) != interactionName)
            {
                return;
            }

            interactionEvent.Source.ClientInteract(interactionEvent, interaction.Interaction, new InteractionReference(referenceId));
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

            return GetInteractionsFromTargets(source, targets, interactionEvent);
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

        /// <summary>
        /// Generates all possible interactions, given both a source and targets
        /// </summary>
        /// <param name="source">The interaction source</param>
        /// <param name="targets">The interaction targets</param>
        /// <param name="interactionEvent">The interaction event data</param>
        /// <returns>A list of all possible interaction entries</returns>
        [ServerOrClient]
        private List<InteractionEntry> GetInteractionsFromTargets(IInteractionSource source, List<IInteractionTarget> targets, InteractionEvent interactionEvent)
        {
            List<InteractionEntry> interactions = new();
            Vector3 point = interactionEvent.Point;

            // Generate interactions on targets
            foreach (IInteractionTarget target in targets)
            {
                InteractionEvent e = new(source, target, point);
                IInteraction[] targetInteractions = target.CreateTargetInteractions(e);

                foreach (IInteraction interaction in targetInteractions)
                {
                    InteractionEntry entry = new(target, interaction);
                    interactions.Add(entry);
                }
            }

            // Allow the source to add its own interactions
            source.CreateSourceInteractions(targets.ToArray(), interactions);

            // Filter interactions to possible ones
            List<InteractionEntry> interactionsFromTargets = new();
            foreach (InteractionEntry entry in interactions)
            {
                InteractionEvent e = new(source, entry.Target, point);

                if (entry.Interaction.CanInteract(e))
                {
                    interactionsFromTargets.Add(entry);
                }
            }

            return interactionsFromTargets;
        }

        [ServerOrClient]
        private IInteractionSource GetActiveInteractionSource()
        {
            IHandsController handsController = GetComponent<IHandsController>();
            var interactionSource = handsController.GetActiveInteractionSource();

            return interactionSource;
        }

        [ServerRpc]
        private void CmdRunInventoryInteraction(GameObject target, GameObject sourceObject, int index, string interactionName)
        {
            IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
            InteractionEvent interactionEvent = new(source, null, source.GameObject.transform.position);

            List<InteractionEntry> entries = GetInteractionsFromTargets(source, targets, interactionEvent);

            // TODO: Validate access to inventory

            // Check for valid interaction index
            if (index < 0 || entries.Count <= index)
            {
                Log.Error(target, "Inventory interaction with invalid index {index}", Logs.Generic, index);

                return;
            }

            InteractionEntry chosenEntry = entries[index];
            interactionEvent.Target = chosenEntry.Target;

            if (chosenEntry.Interaction.GetName(interactionEvent) != interactionName)
            {
                Log.Error(target, "Interaction at index {index} did not have the expected name of {interactionName}",
                    Logs.Generic, index, interactionName);

                return;
            }

            InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, chosenEntry.Interaction);
            if (chosenEntry.Interaction is IClientInteractionSource)
            {
                RpcExecuteClientInventoryInteraction(target, sourceObject, interactionName, reference.Id);
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
        private void RpcExecuteClientInventoryInteraction(GameObject target, GameObject sourceObject, string interactionName, int referenceId)
        {
            if (IsServer)
            {
                return;
            }

            IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
            InteractionEvent interactionEvent = new(source, new InteractionTargetGameObject(target));
            List<InteractionEntry> entries = GetInteractionsFromTargets(source, targets, interactionEvent);

            InteractionEntry chosenInteraction = entries.Find(entry => entry.Interaction.GetName(interactionEvent) == interactionName);
            interactionEvent.Target = chosenInteraction.Target;

            interactionEvent.Source.ClientInteract(interactionEvent, chosenInteraction.Interaction, new InteractionReference(referenceId));
        }
    }
}