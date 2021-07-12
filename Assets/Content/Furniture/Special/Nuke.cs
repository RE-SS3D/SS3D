using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Server.Round;
using Telepathy;
using TMPro;
using UnityEngine;

namespace SS3D.Content.Furniture.Special
{
    /// <summary>
    /// Handles simple end round functionality for now.
    /// Does a loud boom when clicked for some reason.
    /// </summary>
    public class Nuke : InteractionTargetNetworkBehaviour
    {
        /// <summary>
        /// time it takes to explosion from the activation
        /// </summary>
        public int explosionDelay = 10;

        /// <summary>
        /// current time to explosion
        /// </summary>
        public int currentTimerSeconds;
        
        /// <summary>
        /// is the countdown activated
        /// </summary>
        public bool countdownActive;
        
        /// <summary>
        /// is the nuke going to explode?
        /// </summary>
        public bool activated;

        /// <summary>
        /// sound that makes when the nuke beeps
        /// </summary>
        public AudioClip beepSound;

        /// <summary>
        /// sound that makes when the nu
        /// </summary>
        public AudioClip explosionSound;

        /// <summary>
        /// component that plays audio
        /// </summary>
        public AudioSource audioSource;

        /// <summary>
        /// could we still save the station
        /// </summary>
        public bool canDefuse;

        /// <summary>
        /// temporary shit until we have AssetData
        /// </summary>
        public Sprite interactionIcon;

        /// <summary>
        /// the prefab for the timer UI that will appear above the nuke
        /// </summary>
        public GameObject timerPrefab;
        // timer text
        public TMP_Text timerText;

        /// <summary>
        /// Thrown when the nuke state is changed
        /// </summary>
        public static event System.Action<Entity, bool> NukeStateChanged; 
        /// <summary>
        ///  Activates the nuke explosion sequence
        /// </summary>
        [Server]
        public void StartDetonationSequence()
        {
            // sets the time to explosion
            currentTimerSeconds = explosionDelay;
            countdownActive = true;
            
            // start beep coroutine
            StartCoroutine(NukeActivationCountdown());
        }

        // Requests the nuclear detonation
        [Command(ignoreAuthority = true)]
        public void CmdStartDetonationSequence()
        {
            StartDetonationSequence();
        }

        // Handles the nuclear explosion countdown in the server
        [Server]
        private IEnumerator NukeActivationCountdown()
        {
            while (currentTimerSeconds > 0 && countdownActive)
            {
                RpcNukeCountdownTick();
                currentTimerSeconds--;

                // waits 1 second
                yield return new WaitForSeconds(1);
            }

            // Handles the nuclear explosion
            if (activated)
                Boom();

            // Ends the round if the it should end on nuclear explosions
            // TODO: A distance check from the station so we can throw nukes in the space 
            // TODO: without exploding everything, I should say this should be really really far
            RoundManager roundManager = RoundManager.singleton;

            if (roundManager.endOnNuclearExplosion)
                roundManager.CmdEndRound();
        }

        [ClientRpc]
        public void RpcNukeCountdownTick()
        {
            // handles the timer UI
            if (timerText == null)
            {
                // spawns the timer
                GameObject timerInstance = Instantiate(timerPrefab, gameObject.transform);
                // offset it because the Nuke's origin(pivot) is on the base because its a fixture
                timerInstance.transform.localPosition = new Vector3(0, 1.7f, 0);
                timerText = timerInstance.GetComponentInChildren<TMP_Text>();
            }
            
            // updates the timer UI
            timerText.text = currentTimerSeconds.ToString();
            Debug.Log("Nuclear detonation countdown: " + currentTimerSeconds);

            //if (currentTimerSeconds > 0)
            audioSource.PlayOneShot(beepSound);
        }

        // Handle the explosion on the server
        [Server]
        public void Boom()
        {
            audioSource.maxDistance = Single.MaxValue;
            //audioSource.PlayOneShot(explosionSound);
            RpcBoom();
        }

        // Handle the explosion on the client
        [ClientRpc]
        public void RpcBoom()
        {
            if (isServer) return;
            audioSource.maxDistance = Single.MaxValue;
            //audioSource.PlayOneShot(explosionSound);
        }

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new List<IInteraction>();
            
            ActivateNukeInteraction activateNukeInteraction = new ActivateNukeInteraction
            {
                icon = interactionIcon
            };

            if (!countdownActive)
                interactions.Add(activateNukeInteraction);

            return interactions.ToArray();
        }

        public class ActivateNukeInteraction : SimpleInteraction
        {
            public override bool CanInteract(InteractionEvent interactionEvent)
            {
                if (interactionEvent.Target is Nuke nuke)
                {
                    if (!InteractionExtensions.RangeCheck(interactionEvent))
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }

            public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
            {
                if (interactionEvent.Target is Nuke nuke)
                {
                    // Activates the nuke explosion sequence
                    Nuke.NukeStateChanged.Invoke(interactionEvent.Source.GetEntity(), nuke.activated);
                    Debug.Log("Nuke activated!!");
                    //TODO: Implement simple code validation
                    nuke.CmdStartDetonationSequence();
                    return true;
                }

                return false;
            }
        }
    }
}
