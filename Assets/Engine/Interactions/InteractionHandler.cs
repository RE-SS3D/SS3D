using System.Collections.Generic;
using System.Linq;
using Mirror;
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

        public void Update()
        {
            // Ensure that mouse isn't over ui (game objects aren't tracked by the eventsystem, so ispointer would return false
            if (!isLocalPlayer || Camera.main == null || EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetButtonDown("Click"))
            {
                if(activeMenu != null) {
                    Destroy(activeMenu.gameObject);
                    return;
                }
                
                // Run the most prioritised action
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);

                if (viableInteractions.Count > 0)
                {
                    CmdRunInteraction(ray, 0, viableInteractions[0].GetName(interactionEvent));
                }
                    
            }
            else if (Input.GetButtonDown("Secondary Click"))
            {
                if(activeMenu != null ) {
                    Destroy(activeMenu.gameObject);
                }

                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);

                if (viableInteractions.Count < 1)
                {
                    return;
                }

                // Create a menu that will run the given action when clicked
                var obj = Instantiate(menuPrefab, transform);
                activeMenu = obj.GetComponent<UI.MenuUI>();

                activeMenu.Position = Input.mousePosition;
                activeMenu.Event = interactionEvent;
                activeMenu.Interactions = viableInteractions;
                activeMenu.onSelect = interaction =>
                {
                    CmdRunInteraction(ray, viableInteractions.IndexOf(interaction),
                            interaction.GetName(interactionEvent));
                };
            }
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
            List<IInteraction> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);
            if (index >= viableInteractions.Count)
            {
                Debug.LogError($"Interaction received from client {gameObject.name} can not occur! Server-client misalignment.");
                return;
            }
            var chosenInteraction = viableInteractions[index];

            if (chosenInteraction.GetName(interactionEvent) != name)
            {
                Debug.LogError($"Interaction at index {index} did not have the expected name of {name}");
                return;
            }

            InteractionReference reference = interactionEvent.Source.Interact(interactionEvent, chosenInteraction);
            if (chosenInteraction.CreateClient(interactionEvent) != null)
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
            List<IInteraction> viableInteractions = GetViableInteractions(ray, out InteractionEvent interactionEvent);
            if (index >= viableInteractions.Count)
            {
                Debug.LogWarning($"Interaction received from server can not occur! Server-client misalignment on object {gameObject.name}.", this);
                return;
            }
            var chosenInteraction = viableInteractions[index];

            if (chosenInteraction.GetName(interactionEvent) != name)
            {
                return;
            }
            
            interactionEvent.Source.ClientInteract(interactionEvent, chosenInteraction, new InteractionReference(referenceId));
        }

        private List<IInteraction> GetViableInteractions(Ray ray, out InteractionEvent interactionEvent)
        {
            IInteractionSource source = GetActiveInteractionSource();
            if (source == null)
            {
                interactionEvent = null;
                return new List<IInteraction>();
            }
            
            List<IInteractionTarget> targets = new List<IInteractionTarget>();
            Vector3 point = Vector3.zero;
            if (Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, selectionMask))
            {
                point = hit.point;
                GameObject targetGo = hit.transform.gameObject;
                targets.AddRange(targetGo.GetComponents<IInteractionTarget>().Where(x =>
                    (x as MonoBehaviour)?.enabled != false && source.CanInteractWithTarget(x)));
                if (targets.Count < 1)
                {
                    targets.Add(new InteractionTargetGameObject(targetGo));
                }
            }

            // TODO: Please god have mercy on my rotten soul
            interactionEvent = new InteractionEvent(source, targets[0], point);
            
            IInteraction[] availableInteractions =
                source.GenerateInteractions(targets.ToArray());
            
            InteractionEvent @event = interactionEvent;
            return availableInteractions.Where(i => i.CanInteract(@event)).ToList();
        }

        private IInteractionSource GetActiveInteractionSource()
        {
            IToolHolder toolHolder = GetComponent<IToolHolder>();
            IInteractionSource activeTool = toolHolder?.GetActiveTool();
            return activeTool ?? GetComponent<IInteractionSource>();
        }
        
        private UI.MenuUI activeMenu = null;
    }
}
