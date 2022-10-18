﻿using System.Collections.Generic;
using System.Linq;
using Coimbra;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Interactions
{
    /// <summary>
    /// Attached to the player, initiates interactions.
    /// </summary>
    public class InteractionHandler : NetworkedSpessBehaviour
    {
        /// <summary>
        /// Mask for physics to use when finding targets
        /// </summary>
        [Tooltip("Mask for physics to use when finding targets")]
        [SerializeField] private LayerMask _selectionMask = 0;

        // Must be the one ui context menu prefab object
        [SerializeField] private GameObject _menuPrefab;

        private Camera _camera;
        private RadialInteractionMenuView _activeMenu;

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            // Ensure that mouse isn't over ui (game objects aren't tracked by the eventsystem, so ispointer would return false
            if (!IsOwner || _camera == null || EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetButtonDown("Click"))
            {
                ProcessPrimaryClick();
            }

            else if (Input.GetButtonDown("Secondary Click"))
            {
                ProcessSecondaryClick();
            }

            if (Input.GetButtonDown("Activate"))
            {
                ProcessUse();
            }
        }

        private void ProcessUse()
        {
            // Activate item in selected hand
            // Hands hands = GetComponent<Hands>();
            // if (hands != null )
            // {
            //     Item item = hands.ItemInHand;
            //     if (item != null)
            //     {
            //         InteractInHand(item.gameObject, gameObject);
            //     }
            // }
        }

        private void ProcessSecondaryClick()
        {
            if (_activeMenu != null)
            {
                _activeMenu.Destroy();
            }

            if (Input.GetButton("Alternate"))
            {
                // Hands hands = GetComponent<Hands>();
                // if (hands != null )
                // {
                //     Item item = hands.ItemInHand;
                //     if (item != null)
                //     {
                //         InteractInHand(item.gameObject, gameObject, true);
                //     }
                // }
            }
            else
            {
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                List<InteractionEntry> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);
                if (viableInteractions.Select(x => x.Interaction).ToList().Count <= 0)
                {
                    return;
                }

                // Create a menu that will run the given action when clicked
                GameObject menu = Instantiate(_menuPrefab, Root);
                _activeMenu = menu.GetComponentInChildren<RadialInteractionMenuView>();

                _activeMenu.Position = Input.mousePosition;
                _activeMenu.Event = interactionEvent;
                _activeMenu.Interactions = viableInteractions.Select(x => x.Interaction).ToList();
                _activeMenu.OnSelect = interaction =>
                {
                    CmdRunInteraction(ray, viableInteractions.FindIndex(x => x.Interaction == interaction),
                        interaction.GetName(interactionEvent));
                };
            }
        }

        private void ProcessPrimaryClick()
        {
            if (_activeMenu != null)
            {
                _activeMenu.Destroy(); 
                return;
            }

            // Run the most prioritised action
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            List<InteractionEntry> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);

            if (viableInteractions.Count <= 0) return;
            interactionEvent.Target = viableInteractions[0].Target;
            CmdRunInteraction(ray, 0, viableInteractions[0].Interaction.GetName(interactionEvent));
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
            IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
            if (source == null)
            {
                return;
            }
            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
            InteractionEvent interactionEvent = new(source, null);
            List<InteractionEntry> entries = GetInteractionsFromTargets(source, targets, interactionEvent);
            if (entries.Count < 1)
            {
                return;
            }

            interactionEvent.Target = entries[0].Target;
            if (showMenu && entries.Select(x => x.Interaction).ToList().Count > 0)
            {
                GameObject menu = Instantiate(_menuPrefab, transform.root.transform);
                _activeMenu = menu.GetComponentInChildren<RadialInteractionMenuView>();

                Vector3 mousePosition = Input.mousePosition;
                mousePosition.y = Mathf.Max(menu.transform.GetChild(0).GetComponent<RectTransform>().rect.height, mousePosition.y);
                _activeMenu.Position = mousePosition;
                _activeMenu.Event = interactionEvent;
                _activeMenu.Interactions = entries.Select(x => x.Interaction).ToList();
                _activeMenu.OnSelect = interaction =>
                {
                    CmdRunInventoryInteraction(target, sourceObject,
                        entries.FindIndex(x => x.Interaction == interaction),
                        interaction.GetName(interactionEvent));
                };
            }
            else
            {
                CmdRunInventoryInteraction(target, sourceObject, 0, entries[0].Interaction.GetName(interactionEvent));
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

        /**
         * <summary>
         * Runs an interaction (chosen on the client) on the server.
         * <br />
         * For reasons of serialization and security, some code is re-run
         * </summary>
         * <param name="ray">
         * The ray the click came from. RaycastHit is not serializable and this ensures
         * that a user can't try to interact with something that should be invisible.
         * </param>
         * <param name="index">
         * The index into the prioritised interaction list this interaction is at
         * </param>
         * <param name="name">
         * To confirm the interaction is the correctly selected one.
         * </param>
         */
        [ServerRpc]
        private void CmdRunInteraction(Ray ray, int index, string name)
        {
            List<InteractionEntry> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);
            if (index >= viableInteractions.Count)
            {
                Debug.LogError($"Interaction received from client {gameObject.name} can not occur! Server-client misalignment.");
                return;
            }
            InteractionEntry chosenEntry = viableInteractions[index];
            interactionEvent.Target = chosenEntry.Target;

            if (chosenEntry.Interaction.GetName(interactionEvent) != name)
            {
                Debug.LogError($"Interaction at index {index} did not have the expected name of {name}");
                return;
            }

            InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, chosenEntry.Interaction);
            if (chosenEntry.Interaction.CreateClient(interactionEvent) != null)
            {
                RpcExecuteClientInteraction(ray, index, name, reference.Id);
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
            if (Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, _selectionMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                GameObject targetGo = hit.transform.gameObject;
                targets = GetTargetsFromGameObject(source, targetGo);
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
            List<IInteractionTarget> targets = new();
            // Get all target components which are not disabled and the source can interact with
            targets.AddRange(gameObject.GetComponents<IInteractionTarget>().Where(x =>(x as MonoBehaviour)?.enabled != false && source.CanInteractWithTarget(x)));
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
        /// <param name="event">The interaction event data</param>
        /// <returns>A list of all possible interaction entries</returns>
        private List<InteractionEntry> GetInteractionsFromTargets(IInteractionSource source,
            List<IInteractionTarget> targets, InteractionEvent @event)
        {
            // Generate interactions on targets
            List<InteractionEntry> interactions = targets.SelectMany(t => 
                t.GenerateInteractionsFromTarget(new InteractionEvent(source, t, @event.Point))
                    .Select(i => new InteractionEntry(t, i))
            ).ToList();
            
            // Allow the source to add its own interactions
            source.GenerateInteractionsFromSource(targets.ToArray(), interactions);
            
            // Filter interactions to possible ones
            return interactions.Where(i => i.Interaction.CanInteract(new InteractionEvent(source, i.Target, @event.Point))).ToList();
        }

        private IInteractionSource GetActiveInteractionSource()
        {
            IToolHolder toolHolder = GetComponent<IToolHolder>();
            IInteractionSource activeTool = toolHolder?.GetActiveTool();
            return activeTool ?? GetComponent<IInteractionSource>();
        }
    }
}
