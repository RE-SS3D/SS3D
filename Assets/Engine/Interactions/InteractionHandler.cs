using UnityEngine;
using System.Collections;
using Mirror;
using UnityEngine.EventSystems;
using SS3D.Engine.Inventory.Extensions;
using System.Collections.Generic;
using System.Linq;
using System;

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
                return;

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
            else if (continuousInteraction != null && Input.GetButton("Click"))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                CmdContinueInteraction(ray);
            }
            else if (continuousInteraction != null && Input.GetButtonUp("Click")) 
            {
                CmdEndInteraction();
                continuousInteraction = null;
            }
            else if (Input.GetButtonDown("Secondary Click"))
            {
                if(activeMenu != null) {
                    Destroy(activeMenu.gameObject);
                }

                // Create a menu that will run the given action when clicked
                var obj = Instantiate(menuPrefab, transform);
                activeMenu = obj.GetComponent<UI.MenuUI>();

                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var viableInteractions = GetViableInteractions(ray);

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
            var chosenInteraction = GetViableInteractions(ray)[index];

            // Ensure the interaction can happen
            if (!chosenInteraction.CanInteract())
            {
                Debug.LogError($"Interaction recieved from client {gameObject.name} can not occur! Server-client misalignment.");
                return;
            }

            if (chosenInteraction.Name != name)
            {
                Debug.LogError($"Interaction at index {index} did not have the expected name of {name}");
                return;
            }

            chosenInteraction.Interact();

            if(chosenInteraction is ContinuousInteraction)
                continuousInteraction = chosenInteraction as ContinuousInteraction;
        }

        [Command]
        private void CmdContinueInteraction(Ray ray)
        {
            Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, selectionMask);

            var tool = GetActiveTool();
            var target = hit.transform.gameObject;

            continuousInteraction.Event = new InteractionEvent(tool, target, hit, connectionToClient);
            bool shouldContinue = continuousInteraction.ContinueInteracting();

            if(!shouldContinue) {
                continuousInteraction.EndInteraction();

                // Inform the client we stopped the 
                TargetClearContinuousInteraction(connectionToClient);
                continuousInteraction = null;
            }
        }

        [Command]
        private void CmdEndInteraction()
        {
            if(continuousInteraction == null)
            {
                Debug.LogError("CmdEndInteraction was called despite not having a continuous interaction");
                return;
            }

            continuousInteraction.EndInteraction();
        }

        [TargetRpc]
        private void TargetClearContinuousInteraction(NetworkConnection target)
        {
            continuousInteraction = null;
        }

        private List<Interaction> GetViableInteractions(Ray ray)
        {
            // Get the target and tool
            if (!Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, selectionMask))
                return new List<Interaction>();

            var target = hit.transform.gameObject;
            var tool = GetActiveTool();

            var interactionEvent = new InteractionEvent(tool, target, hit);

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
        private ContinuousInteraction continuousInteraction = null;
        private UI.MenuUI activeMenu = null;
    }
}
