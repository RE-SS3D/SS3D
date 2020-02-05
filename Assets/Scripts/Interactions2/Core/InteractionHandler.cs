using UnityEngine;
using System.Collections;
using Mirror;
using UnityEngine.EventSystems;
using Inventory.Custom;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Interactions2.Core
{
    /**
     * <summary>Attached to the player, initiates interactions.</summary>
     */
    public class InteractionHandler : NetworkBehaviour
    {
        private enum InteractionSource
        {
            Target,
            Tool,
            Universal
        }

        /// <summary>Interactions which should be included at all applicable times</summary>
        [SerializeField]
        [Tooltip("Interactions which should be included at all applicable times")]
        private InteractionSO[] universalInteractions = null;
        /// <summary>Mask for physics to use when finding targets</summary>
        [SerializeField]
        [Tooltip("Mask for physics to use when finding targets")]
        private LayerMask selectionMask = 0;

        public void Update()
        {
            // Ensure that mouse isn't over ui (game objects aren't tracked by the eventsystem, so ispointer would return false
            if (!isLocalPlayer || Camera.main == null || EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetButtonDown("Click"))
                RunDefaultInteraction();
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
        }

        /**
         * Finds and determines the interaction to run.
         */
        private void RunDefaultInteraction()
        {
            // Get the target and tool
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, selectionMask))
                return;

            var target = hit.transform.gameObject;
            var tool = GetActiveTool();

            var targetInteractions = GetInteractionsFrom(target);
            var toolInteractions = GetInteractionsFrom(tool);

            // Collect interactions
            List<Interaction> availableInteractions = targetInteractions.Concat(toolInteractions).Concat(universalInteractions).ToList();

            // Filter to usable ones
            List<Interaction> viableInteractions = availableInteractions.Where(interaction => interaction.CanInteract(tool, target, hit)).ToList();

            // Order interactions
            // TODO: Prioritise interactions

            if (viableInteractions.Count == 0)
                return;
            var chosenInteraction = viableInteractions[0];

            // TODO: Condense this code
            var source = InteractionSource.Universal;
            var indexInSource = 0;
            if (targetInteractions.Contains(chosenInteraction)) {
                source = InteractionSource.Target;
                indexInSource = targetInteractions.IndexOf(chosenInteraction);
            }
            else if (toolInteractions.Contains(chosenInteraction)) {
                source = InteractionSource.Tool;
                indexInSource = toolInteractions.IndexOf(chosenInteraction);
            }
            else {
                indexInSource = Array.IndexOf(universalInteractions, chosenInteraction);
            }

            if (chosenInteraction is ContinuousInteraction)
                continuousInteraction = chosenInteraction as ContinuousInteraction;

            CmdRunInteraction(ray, source, indexInSource);
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
         * <param name="interactionSource">
         * What object the interaction came from.
         * </param>
         * <param name="indexInSource">
         * The index of the interaction in the (original) interaction list of the given object
         * </param>
         * TODO: Some third parameter about interaction (name?) to confirm that is the correct interaction
         */
        [Command]
        private void CmdRunInteraction(Ray ray, InteractionSource interactionSource, int indexInSource)
        {
            Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, selectionMask);

            var target = hit.transform.gameObject;
            var tool = GetActiveTool();

            // Get the correct list of interactions based on the source
            List<Interaction> interactions = null;
            switch(interactionSource)    
            {
                case InteractionSource.Target:
                    interactions = GetInteractionsFrom(target);
                    break;
                case InteractionSource.Tool:
                    interactions = GetInteractionsFrom(tool);
                    break;
                case InteractionSource.Universal:
                    interactions = universalInteractions.ToList<Interaction>();
                    break;
            }

            var chosenInteraction = interactions[indexInSource];

            // Ensure the interaction can happen
            if(!chosenInteraction.CanInteract(tool, target, hit))
            {
                Debug.LogError($"Interaction recieved from client {gameObject.name} can not occur! Server-client misalignment.");
                return;
            }

            chosenInteraction.ConnectionToClient = connectionToClient;
            chosenInteraction.Interact(tool, target, hit);

            if(chosenInteraction is ContinuousInteraction)
                continuousInteraction = chosenInteraction as ContinuousInteraction;
        }

        [Command]
        private void CmdContinueInteraction(Ray ray)
        {
            Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, selectionMask);

            var tool = GetActiveTool();
            var target = hit.transform.gameObject;

            bool shouldContinue = continuousInteraction.ContinueInteracting(tool, target, hit);

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

        private GameObject GetActiveTool()
        {
            // TODO: Hands should extend a ToolHolder or something like that.
            return GetComponent<Hands>().GetItemInHand()?.gameObject ?? GetComponent<Hands>().gameObject;
        }

        private static List<Interaction> GetInteractionsFrom(GameObject gameObject)
        {
            return gameObject.GetComponent<InteractionAttacher>()?.Interactions
                ?? gameObject.GetComponents<Interaction>().ToList();
        }

        // Server and client track these seperately
        private ContinuousInteraction continuousInteraction = null;
    }
}
