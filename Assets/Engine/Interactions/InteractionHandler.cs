using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using SS3D.Engine.Inventory.Extensions;
using SS3D.Engine.Interactions.Extensions;

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
            // Run server interactions
            if (isServer && activeInteraction != null)
            {
                bool continueInteracting = activeInteraction.ContinueInteracting();
                if (!continueInteracting)
                {
                    activeInteraction.EndInteraction();
                    activeInteraction = null;
                    TargetSetDoingInteraction(connectionToClient, false);
                }
            }
            
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
                var viableInteractions = GetViableInteractions(ray);

                if (viableInteractions.Count > 0)
                    CmdRunInteraction(ray, 0, viableInteractions[0].Name);
            }
            else if (Input.GetButtonDown("Secondary Click"))
            {
                if(activeMenu != null ) {
                    Destroy(activeMenu.gameObject);
                }

                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var viableInteractions = GetViableInteractions(ray);

                if (viableInteractions.Count < 1)
                {
                    return;
                }

                // Create a menu that will run the given action when clicked
                var obj = Instantiate(menuPrefab, transform);
                activeMenu = obj.GetComponent<UI.MenuUI>();

                activeMenu.Position = Input.mousePosition;
                activeMenu.Interactions = viableInteractions;
                activeMenu.onSelect = interaction => CmdRunInteraction(ray, viableInteractions.IndexOf(interaction), interaction.Name);
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
            List<Interaction> viableInteractions = GetViableInteractions(ray);
            if (index >= viableInteractions.Count)
            {
                Debug.LogError($"Interaction recieved from client {gameObject.name} can not occur! Server-client misalignment.");
                return;
            }
            var chosenInteraction = viableInteractions[index];

            if (chosenInteraction.Name != name)
            {
                Debug.LogError($"Interaction at index {index} did not have the expected name of {name}");
                return;
            }

            chosenInteraction.Interact();

            if (chosenInteraction is ContinuousInteraction interaction)
            {
                // Make sure to end any current interaction
                activeInteraction?.EndInteraction();
                activeInteraction = interaction;
                // Notify client of interaction
                TargetSetDoingInteraction(connectionToClient, true);
            }
                
        }

        [Command]
        private void CmdContinueInteraction(Ray ray)
        {
            Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, selectionMask);

            var tool = GetActiveTool();
            var target = hit.transform.gameObject;

            activeInteraction.Event = new InteractionEvent(tool, target, hit, connectionToClient);
            bool shouldContinue = activeInteraction.ContinueInteracting();

            if(!shouldContinue) {
                activeInteraction.EndInteraction();

                // Inform the client we stopped the 
                activeInteraction = null;
            }
        }

        [Command]
        private void CmdEndInteraction()
        {
            if(activeInteraction == null)
            {
                Debug.LogError("CmdEndInteraction was called despite not having a active interaction");
                return;
            }

            activeInteraction.EndInteraction();
        }

        [TargetRpc]
        private void TargetSetDoingInteraction(NetworkConnection target, bool isDoing)
        {
            doingInteraction = isDoing;
        }

        private List<Interaction> GetViableInteractions(Ray ray)
        {
            // Get the target and tool
            if (!Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, selectionMask))
                return new List<Interaction>();

            var target = hit.transform.gameObject;
            var tool = GetActiveTool();

            var interactionEvent = new InteractionEvent(tool, target, hit, isServer ? connectionToClient : null);

            // Collect interactions
            List<Interaction> availableInteractions = 
                (tool == gameObject
                    ? new List<Interaction>()
                    : tool.GetAllInteractions(interactionEvent)
                )
                    .Concat(target.GetAllInteractions(interactionEvent))
                    .Concat(gameObject.GetAllInteractions(interactionEvent))
                    .ToList();

            availableInteractions.ForEach(interaction => interaction.Event = interactionEvent);

            // Order interactions
            // TODO: Prioritise interactions

            // Filter to usable ones
            return availableInteractions
                .Where(interaction => interaction.CanInteract())
                .ToList();
        }

        private GameObject GetActiveTool()
        {
            // TODO: Hands should extend a ToolHolder or something like that.
            return GetComponent<Hands>().GetItemInHand()?.gameObject ?? GetComponent<Hands>().gameObject;
        }

        // Server and client track these seperately
        private ContinuousInteraction activeInteraction = null;
        private UI.MenuUI activeMenu = null;
        // Tracked on client, causing it to notify the server
        private bool doingInteraction = false;
    }
}
