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
        // time it takes to explosion from the activation
        public int explosionDelay = 10;

        // current time to explosion
        public int currentTimerSeconds;
        
        // is the countdown activated
        public bool countdownActive;
        
        // is the nuke going to explode?
        public bool activated;

        // sound that makes when the nuke beeps
        public AudioClip beepSound;

        // sound that makes when the nuk
        public AudioClip explosionSound;

        // component that plays audio
        public AudioSource audioSource;

        // could we still save the station
        public bool canDefuse;

        // temporary shit until we have AssetData
        public Sprite interactionIcon;

        // the prefab for the timer UI that will appear above the nuke
        public GameObject timerPrefab;
        // timer text
        public TMP_Text timerText;
        
        // Activates the nuke explosion sequence
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
            if (timerText == null)
            {
                // spawns the timer
                GameObject timerInstance = Instantiate(timerPrefab, gameObject.transform);
                // offset it because the Nuke's origin(pivot) is on the base because its a fixture
                timerInstance.transform.localPosition = new Vector3(0, 1.7f, 0);
                timerText = timerInstance.GetComponentInChildren<TMP_Text>();
            }

            while (currentTimerSeconds > 0 && countdownActive)
            {
                // plays the beeping sound each second
                //audioSource.PlayOneShot(beepSound);
                // then plays it in the client
                RpcNukeCountdownTick();

                // updates the timer UI
                timerText.text = currentTimerSeconds.ToString();
                Debug.Log(" Nuclear detonation countdown: " + currentTimerSeconds);
                currentTimerSeconds--;

                // waits 1 second
                yield return new WaitForSeconds(1);
            }

            // Handles the nuclear explosion
            if (activated)
                Boom();

            // Ends the round if the it should end on nuclear explosions
            // TODO: A distance check from the station so we can throw nukes in the space without 
            // TODO: without exploding everything, I should say this should be really really far
            RoundManager roundManager = RoundManager.singleton;

            if (roundManager.endOnNuclearExplosion)
                roundManager.CmdEndRound();
        }

        // Handles the nuclear explosion countdown in the client
        [ClientRpc]
        public void RpcNukeCountdownTick()
        {
            if (isServer) return;
            
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
            //audioSource.PlayOneShot(beepSound);
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

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
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

                    //TODO: Implement simple code validation
                    nuke.CmdStartDetonationSequence();
                    return true;
                }

                return false;
            }
        }
    }
}
