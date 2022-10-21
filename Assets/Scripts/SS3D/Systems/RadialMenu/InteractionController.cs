﻿using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Storage.Containers;
using SS3D.Systems.Storage.Items;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Systems.RadialMenu
{
    /// <summary>
    /// Attached to the player, initiates interactions.
    /// </summary>
    public class InteractionController : NetworkedSpessBehaviour
    {
        /// <summary>
        /// Mask for physics to use when finding targets
        /// </summary>
        [Tooltip("Mask for physics to use when finding targets")]
        [SerializeField] private LayerMask _selectionMask = 0;

        private Camera _camera;
        private RadialInteractionView _radialView;

        protected override void OnStart()
        {
            base.OnStart();

            _radialView = GameSystems.Get<RadialInteractionView>();
            _camera = Camera.main;
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            if (!IsOwner || _camera == null || EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetButtonDown("Primary Click"))
            {
                ProcessPrimaryClick();
            }

            else if (Input.GetButtonDown("Secondary Click"))
            {
                ProcessSecondaryClick();
            }

            if (Input.GetButtonDown("Use"))
            {
                ProcessUse();
            }
        }

        private void ProcessUse()
        {
            // Activate item in selected hand
            Hands hands = GetComponent<Hands>();
            if (hands == null)
            {
                return;
            }

            Item item = hands.ItemInHand;
            if (item != null)
            {
                InteractInHand(item.gameObject, gameObject);
            }
        }

        private void ProcessSecondaryClick()
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            List<InteractionEntry> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);

            ViewTargetInteractions(viableInteractions, interactionEvent, ray);
        }

        private void ProcessPrimaryClick()
        {
            RunPrimaryInteraction();
        }

        /// <summary>
        /// Runs the most prioritised action
        /// </summary>
        private void RunPrimaryInteraction()
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            List<InteractionEntry> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);

            if (viableInteractions.Count <= 0)
            {
                return;
            }

            InteractionEntry interaction = viableInteractions[0];
            string interactionName = interaction.Interaction.GetName(interactionEvent);
            interactionEvent.Target = interaction.Target;

            CmdRunInteraction(ray, 0, interactionName);
        }

        private void ViewTargetInteractions(List<InteractionEntry> viableInteractions, InteractionEvent interactionEvent, Ray ray)
        {
            List<IInteraction> interactions = viableInteractions.Select(entry => entry.Interaction).ToList();
            
            if (interactions.Count <= 0) { return; }

            void handleInteractionSelected(IInteraction interaction)
            {
                string interactionName = interaction.GetName(interactionEvent);

                int index = viableInteractions.FindIndex(x => x.Interaction == interaction);
                index = Mathf.Clamp(index, 0, int.MaxValue);

                CmdRunInteraction(ray, index, interactionName);
            }

            _radialView.SetInteractions(interactions, interactionEvent, Input.mousePosition);
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

            InteractionEvent interactionEvent = new(source, null);

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
                Vector3 mousePosition = Input.mousePosition;
                mousePosition.y = Mathf.Max(_radialView.RectTransform.rect.height, mousePosition.y);

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

        [ServerRpc]
        private void CmdRunInventoryInteraction(GameObject target, GameObject sourceObject, int index, string name)
        {
            IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
            InteractionEvent interactionEvent = new(source, null);
            List<InteractionEntry> entries = GetInteractionsFromTargets(source, targets, interactionEvent);
            
            // TODO: Validate access to inventory

            // Check for valid interaction index
            if (index < 0 || entries.Count <= index)
            {
                Debug.LogError($"Inventory interaction with invalid index {index}", target);
                return;
            }
            
            InteractionEntry chosenEntry = entries[index];
            interactionEvent.Target = chosenEntry.Target;

            if (chosenEntry.Interaction.GetName(interactionEvent) != name)
            {
                Debug.LogError($"Interaction at index {index} did not have the expected name of {name}", target);
                return;
            }
            
            InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, chosenEntry.Interaction);
            if (chosenEntry.Interaction.CreateClient(interactionEvent) != null)
            {
                RpcExecuteClientInventoryInteraction(target, sourceObject, index, name, reference.Id);
            }
        }

        [ObserversRpc]
        private void RpcExecuteClientInventoryInteraction(GameObject target, GameObject sourceObject, int index, string name, int referenceId)
        {
            IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
            InteractionEvent interactionEvent = new(source, null);
            List<InteractionEntry> entries = GetInteractionsFromTargets(source, targets, interactionEvent);
            
            InteractionEntry chosenInteraction = entries[index];
            interactionEvent.Target = chosenInteraction.Target;
            
            if (chosenInteraction.Interaction.GetName(interactionEvent) != name)
            {
                return;
            }
            
            interactionEvent.Source.ClientInteract(interactionEvent, chosenInteraction.Interaction, new InteractionReference(referenceId));
        }

        /// <summary>
        /// Runs an interaction (chosen on the client) on the server. For reasons of serialization and security, some code is re-run.
        /// </summary>
        /// <param name="ray">The ray the click came from. RaycastHit is not serializable and this ensures hat a user can't try to interact with something that should be invisible.</param>
        /// <param name="index">The index into the prioritised interaction list this interaction is at</param>
        /// <param name="name">To confirm the interaction is the correctly selected one.</param>
        [ServerRpc]
        private void CmdRunInteraction(Ray ray, int index, string interactionName)
        {
            List<InteractionEntry> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);
            if (index >= viableInteractions.Count)
            {
                Debug.LogError($"Interaction received from client {gameObject.name} can not occur! Server-client misalignment.");
                return;
            }
            InteractionEntry chosenEntry = viableInteractions[index];
            interactionEvent.Target = chosenEntry.Target;

            string chosenName = chosenEntry.Interaction.GetName(interactionEvent);

            if (chosenName != interactionName)
            {
                Debug.LogError($"Interaction at index {index} did not have the expected name of {interactionName}");
                return;
            }

            InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, chosenEntry.Interaction);
            if (chosenEntry.Interaction.CreateClient(interactionEvent) != null)
            {
                RpcExecuteClientInteraction(ray, index, interactionName, reference.Id);
            }
            
            // TODO: Keep track of interactions for cancellation
        }

        /// <summary>
        /// Confirms an interaction issued by a client
        /// </summary>
        [ObserversRpc]
        private void RpcExecuteClientInteraction(Ray ray, int index, string name, int referenceId)
        {
            List<InteractionEntry> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);
            if (index >= viableInteractions.Count)
            {
                Debug.LogWarning($"Interaction received from server can not occur! Server-client misalignment on object {gameObject.name}.", this);
                return;
            }
            InteractionEntry chosenInteraction = viableInteractions[index];
            interactionEvent.Target = chosenInteraction.Target;
            
            if (chosenInteraction.Interaction.GetName(interactionEvent) != name)
            {
                return;
            }
            
            interactionEvent.Source.ClientInteract(interactionEvent, chosenInteraction.Interaction, new InteractionReference(referenceId));
        }

        /// <summary>
        /// Gets all possible interactions, given a ray
        /// </summary>
        /// <param name="ray">The ray to use in ray casting</param>
        /// <param name="interactionEvent">The produced interaction event</param>
        /// <returns>A list of possible interactions</returns>
        private List<InteractionEntry> GetViableInteractions(Ray ray, out InteractionEvent interactionEvent)
        {
            // Get source that's currently interacting (eg. hand, tool)
            IInteractionSource source = GetActiveInteractionSource();

            if (source == null)
            {
                interactionEvent = null;
                return new List<InteractionEntry>();
            }
            
            List<IInteractionTarget> targets = new();

            // Raycast to find target game object
            Vector3 point = Vector3.zero;
            bool raycast = Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, _selectionMask, QueryTriggerInteraction.Ignore);
            if (raycast)
            {
                point = hit.point;
                GameObject target = hit.transform.gameObject;
                targets = GetTargetsFromGameObject(source, target);
            }

            interactionEvent = new InteractionEvent(source, targets[0], point);

            return GetInteractionsFromTargets(source, targets, interactionEvent);
        }

        /// <summary>
        /// Gets all valid interaction targets from a game object
        /// </summary>
        /// <param name="source">The source of the interaction</param>
        /// <param name="gameObject">The game objects the interaction targets are on</param>
        /// <returns>A list of all valid interaction targets</returns>
        private List<IInteractionTarget> GetTargetsFromGameObject(IInteractionSource source, GameObject gameObject)
        {
            List<IInteractionTarget> targets = GetComponents<IInteractionTarget>().ToList();

            bool canInteractWith(IInteractionTarget target)
            {
                MonoBehaviour monoBehaviour = target as MonoBehaviour;

                bool objectIsEnabled = monoBehaviour != null && monoBehaviour.enabled;
                bool canInteractWithTarget = source.CanInteractWithTarget(target);

                return objectIsEnabled && canInteractWithTarget;
            }

            IEnumerable<IInteractionTarget> interactionTargets = targets.Where(canInteractWith);

            // Get all target components which are not disabled and the source can interact with
            targets.AddRange(interactionTargets);
            if (targets.Count < 1)
            {
                targets.Add(new InteractionTargetGameObject(gameObject));
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
        private List<InteractionEntry> GetInteractionsFromTargets(IInteractionSource source, List<IInteractionTarget> targets, InteractionEvent interactionEvent)
        {
            List<InteractionEntry> interactions = new();
            Vector3 point = interactionEvent.Point;

            // Generate interactions on targets
            foreach (IInteractionTarget target in targets)
            {
                InteractionEvent e = new(source, target, point);
                IInteraction[] targetInteractions = target.GetTargetInteractions(e);

                foreach (IInteraction interaction in targetInteractions)
                {
                    InteractionEntry entry = new(target, interaction);
                    interactions.Add(entry);
                }
            }

            // Allow the source to add its own interactions
            source.GenerateInteractionsFromSource(targets.ToArray(), interactions);

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

        private IInteractionSource GetActiveInteractionSource()
        {
            IToolHolder toolHolder = GetComponent<IToolHolder>();
            IInteractionSource activeTool = toolHolder?.GetActiveTool();

            IInteractionSource interactionSource = activeTool ?? GetComponent<IInteractionSource>();

            return interactionSource;
        }
    }
}
