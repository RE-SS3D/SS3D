using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Engine.Interactions
{
    /**
     * <summary>Attached to the player, initiates interactions.</summary>
     */
    public class InteractionHandler : NetworkBehaviour
    {
        /// <summary>Mask for physics to use when finding targets</summary>
        [SerializeField]
        [Tooltip("Mask for physics to use when finding targets")]
        private LayerMask selectionMask = 0;

        // Must be the one ui context menu prefab object
        [SerializeField]
        private GameObject menuPrefab = null;

        private Camera camera;

        private void Start()
        {
            camera = CameraManager.singleton.playerCamera;
        }

        public void Update()
        {
            // Ensure that mouse isn't over ui (game objects aren't tracked by the eventsystem, so ispointer would return false
            if (!isLocalPlayer || camera == null || EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetButtonDown("Click"))
            {
                if (activeMenu != null)
                {
                    Destroy(activeMenu.gameObject);
                    return;
                }

                // Run the most prioritised action
                var ray = camera.ScreenPointToRay(Input.mousePosition);
                var viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);

                if (viableInteractions.Count > 0)
                {
                    interactionEvent.Target = viableInteractions[0].Target;
                    CmdRunInteraction(ray, 0, viableInteractions[0].Interaction.GetName(interactionEvent));
                }

            }
            else if (Input.GetButtonDown("Secondary Click"))
            {
                if (activeMenu != null)
                {
                    Destroy(activeMenu.gameObject);
                }

                if (Input.GetButton("Alternate"))
                {
                    Hands hands = GetComponent<Hands>();
                    if (hands != null )
                    {
                        Item item = hands.ItemInHand;
                        if (item != null)
                        {
                            InteractInHand(item.gameObject, gameObject, true);
                        }
                    }
                }
                else
                {
                    var ray = camera.ScreenPointToRay(Input.mousePosition);
                    var viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);
                    if (viableInteractions.Select(x => x.Interaction).ToList().Count > 0)
                    {
                        // Create a menu that will run the given action when clicked
                        var obj = Instantiate(menuPrefab, transform.root.transform);
                        activeMenu = obj.GetComponentInChildren<UI.RadialInteractionMenuUI>();

                        activeMenu.Position = Input.mousePosition;
                        activeMenu.Event = interactionEvent;
                        activeMenu.Interactions = viableInteractions.Select(x => x.Interaction).ToList();
                        activeMenu.onSelect = interaction =>
                        {
                            CmdRunInteraction(ray, viableInteractions.FindIndex(x => x.Interaction == interaction),
                                interaction.GetName(interactionEvent));
                        };
                    }
                }
            }

            if (Input.GetButtonDown("Activate"))
            {
                // Activate item in selected hand
                Hands hands = GetComponent<Hands>();
                if (hands != null )
                {
                    Item item = hands.ItemInHand;
                    if (item != null)
                    {
                        InteractInHand(item.gameObject, gameObject);
                    }
                }
            }
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
            InteractionEvent interactionEvent = new InteractionEvent(source, null);
            List<InteractionEntry> entries = GetInteractionsFromTargets(source, targets, interactionEvent);
            if (entries.Count < 1)
            {
                return;
            }

            interactionEvent.Target = entries[0].Target;
            if (showMenu && entries.Select(x => x.Interaction).ToList().Count > 0)
            {
                var obj = Instantiate(menuPrefab, transform.root.transform);
                activeMenu = obj.GetComponentInChildren<UI.RadialInteractionMenuUI>();

                Vector3 mousePosition = Input.mousePosition;
                mousePosition.y = Mathf.Max(obj.transform.GetChild(0).GetComponent<RectTransform>().rect.height, mousePosition.y);
                activeMenu.Position = mousePosition;
                activeMenu.Event = interactionEvent;
                activeMenu.Interactions = entries.Select(x => x.Interaction).ToList();
                activeMenu.onSelect = interaction =>
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

        [Command]
        private void CmdRunInventoryInteraction(GameObject target, GameObject sourceObject, int index, string name)
        {
            IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
            InteractionEvent interactionEvent = new InteractionEvent(source, null);
            List<InteractionEntry> entries = GetInteractionsFromTargets(source, targets, interactionEvent);
            
            // TODO: Validate access to inventory

            // Check for valid interaction index
            if (index < 0 || entries.Count <= index)
            {
                Debug.LogError($"Inventory interaction with invalid index {index}", target);
                return;
            }
            
            var chosenEntry = entries[index];
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

        [ClientRpc]
        private void RpcExecuteClientInventoryInteraction(GameObject target, GameObject sourceObject, int index, string name, int referenceId)
        {
            IInteractionSource source = sourceObject.GetComponent<IInteractionSource>();
            List<IInteractionTarget> targets = GetTargetsFromGameObject(source, target);
            InteractionEvent interactionEvent = new InteractionEvent(source, null);
            List<InteractionEntry> entries = GetInteractionsFromTargets(source, targets, interactionEvent);
            
            var chosenInteraction = entries[index];
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
        [Command]
        private void CmdRunInteraction(Ray ray, int index, string name)
        {
            List<InteractionEntry> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);
            if (index >= viableInteractions.Count)
            {
                Debug.LogError($"Interaction received from client {gameObject.name} can not occur! Server-client misalignment.");
                return;
            }
            var chosenEntry = viableInteractions[index];
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
        [ClientRpc]
        private void RpcExecuteClientInteraction(Ray ray, int index, string name, int referenceId)
        {
            List<InteractionEntry> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);
            if (index >= viableInteractions.Count)
            {
                Debug.LogWarning($"Interaction received from server can not occur! Server-client misalignment on object {gameObject.name}.", this);
                return;
            }
            var chosenInteraction = viableInteractions[index];
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
            
            List<IInteractionTarget> targets = new List<IInteractionTarget>();
            // Raycast to find target game object
            Vector3 point = Vector3.zero;
            if (Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, selectionMask, QueryTriggerInteraction.Ignore))
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
            List<IInteractionTarget> targets = new List<IInteractionTarget>();
            // Get all target components which are not disabled and the source can interact with
            targets.AddRange(gameObject.GetComponents<IInteractionTarget>().Where(x =>
                (x as MonoBehaviour)?.enabled != false && source.CanInteractWithTarget(x)));
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
                t.GenerateInteractions(new InteractionEvent(source, t, @event.Point))
                    .Select(i => new InteractionEntry(t, i))
            ).ToList();
            
            // Allow the source to add its own interactions
            source.CreateInteractions(targets.ToArray(), interactions);
            
            // Filter interactions to possible ones
            return interactions.Where(i => i.Interaction.CanInteract(new InteractionEvent(source, i.Target, @event.Point))).ToList();
        }

        private IInteractionSource GetActiveInteractionSource()
        {
            IToolHolder toolHolder = GetComponent<IToolHolder>();
            IInteractionSource activeTool = toolHolder?.GetActiveTool();
            return activeTool ?? GetComponent<IInteractionSource>();
        }
        
        private UI.RadialInteractionMenuUI activeMenu = null;
    }
}
