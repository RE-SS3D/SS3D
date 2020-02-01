using UnityEngine;
using System.Collections;
using Mirror;
using UnityEngine.EventSystems;
using Inventory.Custom;
using System.Collections.Generic;
using System.Linq;

namespace Interactions2.Core
{
    /**
     * <summary>Attached to the player, initiates interactions.</summary>
     */
    public class InteractionHandler : NetworkBehaviour
    {
        /// <summary>Interactions which should be included at all applicable times</summary>
        [SerializeField]
        [Tooltip("Interactions which should be included at all applicable times")]
        private InteractionSO[] universalInteractions = null;
        /// <summary>Mask for physics to use when finding targets</summary>
        [SerializeField]
        [Tooltip("Mask for physics to use when finding targets")]
        private LayerMask selectionMask = 0;

        public void OnClick()
        {
            // Get the target and tool
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, float.PositiveInfinity, selectionMask);

            var target = hit.transform.gameObject;
            var tool = GetActiveTool();

            // Collect interactions
            List<Interaction> possibleInteractions = new List<Interaction>();

            foreach (var interaction in GetInteractionsFrom(target)) {
                if (interaction.CanInteract(tool, target, hit))
                    possibleInteractions.Add(interaction);
            }

            if(tool != null) {
                foreach (var interaction in GetInteractionsFrom(tool)) {
                    if (interaction.CanInteract(tool, target, hit))
                        possibleInteractions.Add(interaction);
                }
            }

            if(universalInteractions != null) {
                foreach (var interaction in universalInteractions) {
                    if (interaction.CanInteract(tool, target, hit))
                        possibleInteractions.Add(interaction);
                }
            }

            // Order interactions
            // TODO: Prioritise interactions

            // TODO: Run on server
            if(possibleInteractions.Count == 0)
                return;

            CmdRunInteraction(ray, 0);
        }

        public void Update()
        {
            // Ensure that mouse isn't over ui (game objects aren't tracked by the eventsystem, so ispointer would return false
            if (Input.GetButtonDown("Click") && Camera.main != null && !EventSystem.current.IsPointerOverGameObject())
                OnClick();
        }

        /**
         * <summary>
         * Runs an interaction (chosen on the client) on the server.
         * <br />
         * For reasons of serialization and security, some code is re-run
         * </summary>
         * <param name="ray">
         * The ray the click came from. A RaycastHit is not serializable and it ensures
         * that a user can't try to interact with something that should be invisible.
         * </param>
         * <param name="interactionIndex">
         * The index of the interaction in the interaction list.
         * TODO: Should find a better way of finding the correct interaction
         * </param>
         */
        [Command]
        private void CmdRunInteraction(Ray ray, int interactionIndex)
        {
            RaycastHit hit;
            Physics.Raycast(ray, out hit, float.PositiveInfinity, selectionMask);

            var target = hit.transform.gameObject;
            var tool = GetActiveTool();

            var interactions = GetPossibleInteractions(tool, target, hit);
            var chosenInteraction = interactions[interactionIndex];

            chosenInteraction.ConnectionToClient = connectionToClient;
            chosenInteraction.Interact(tool, target, hit);
        }

        private GameObject GetActiveTool()
        {
            // TODO: Hands should extend a ToolHolder or something like that.
            return GetComponent<Hands>().GetItemInHand()?.gameObject ?? GetComponent<Hands>().gameObject;
        }

        /**
         * <summary>Get all viable interactions</summary>
         */
        private List<Interaction> GetPossibleInteractions(GameObject tool, GameObject target, RaycastHit hit)
        {
            List<Interaction> possibleInteractions = new List<Interaction>();

            foreach (var interaction in GetInteractionsFrom(target)) {
                if (interaction.CanInteract(tool, target, hit))
                    possibleInteractions.Add(interaction);
            }

            if (tool != null) {
                foreach (var interaction in GetInteractionsFrom(tool)) {
                    if (interaction.CanInteract(tool, target, hit))
                        possibleInteractions.Add(interaction);
                }
            }

            if (universalInteractions != null) {
                foreach (var interaction in universalInteractions) {
                    if (interaction.CanInteract(tool, target, hit))
                        possibleInteractions.Add(interaction);
                }
            }

            return possibleInteractions;
        }

        private List<Interaction> GetInteractionsFrom(GameObject gameObject)
        {
            return gameObject.GetComponent<InteractionAttacher>()?.Interactions
                ?? gameObject.GetComponents<InteractionComponent>().ToList<Interaction>();
        }
    }
}
