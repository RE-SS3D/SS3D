﻿using System.Collections;
using System.Collections.Generic;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;
using UnityEngine.XR;

namespace SS3D.Content
{
    /// <summary>
    /// An entity should be used for anything that ressembles the creature idea
    /// it can walk, interact with stuff, be controlled by a player
    /// do combat, etc.
    /// Anything you expect that is a creature should be considered an Entity
    /// </summary>
    public class Entity : MonoBehaviour
    {
        // view range for the FOV
        public float ViewRange = 10f;

        // hands for the interactions
        private Hands hands;

        public Hands Hands
        {
            get
            {
                if (hands == null)
                {
                    hands = GetComponent<Hands>();
                }

                return hands;
            }
            set => hands = value;
        }

        /// <summary>
        /// Checks if this creature can view a game object
        /// </summary>
        /// <param name="otherObject">The game object to view</param>
        public bool CanSee(GameObject otherObject)
        {
            // TODO: This should be based on a health/organ system
            return Vector3.Distance(gameObject.transform.position, otherObject.transform.position) <= ViewRange;
        }

        /// <summary>
        /// Checks if the creature can interact with an object
        /// </summary>
        /// <param name="otherObject">The game object to interact with</param>
        public bool CanInteract(GameObject otherObject)
        {
            Hands hand = Hands;
            if (hand == null)
            {
                return false;
            }

            return hand.GetInteractionRange().IsInRange(hand.InteractionOrigin, otherObject.transform.position);
        }
    }
}
