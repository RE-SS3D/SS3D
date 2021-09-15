using System.Collections.Generic;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;
using Mirror;
using System;
using Object = UnityEngine.Object;
using SS3D.Engine.Interactions;

namespace SS3D.Content
{
    /// <summary>
    /// An entity should be used for anything that ressembles the creature idea
    /// it can walk, interact with stuff, be controlled by a player
    /// do combat, etc.
    /// Anything you expect that is a creature should be considered an Entity
    /// </summary>
    public class Entity : InteractionSourceNetworkBehaviour, IContainerizable
    {

        public List<Trait> traits;

        // view range for the FOV
        public float ViewRange = 10f;

        // hands for the interactions
        private Hands hands;

        [SerializeField] private Vector2Int size;
        [SerializeField] private float volume;

        private Container container;


        public Vector2Int Size
        {
            get => size;
            set => size = value;
        }

        public float Volume
        {
            get => volume;
        }

        public Container Container
        {
            get => container;
            set => SetContainer(value, false, false);
        }

        public Sprite InventorySprite
        {
            get;
        }


        public bool InContainer()
        {
            return container != null;
        }

        public void SetContainer(Container newContainer, bool alreadyAdded, bool alreadyRemoved)
        {
            if (container == newContainer)
            {
                return;
            }

            container?.RemoveItem(this);

            if (!alreadyAdded && newContainer != null)
            {
                newContainer.AddItem(this);
            }

            container = newContainer;
        }

        public void SetContainerUnchecked(Container newContainer)
        {
            container = newContainer;
        }
        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public List<Trait> Traits
        {
            get => traits;
            set => traits = value;
        }

        public string ItemId
        {
            get;
        }


        public void Destroy()
        {
            Container = null;

            if (isServer)
            {
                NetworkServer.Destroy(gameObject);
            }
            else
            {
                Object.Destroy(gameObject);
            }
        }

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
