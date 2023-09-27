using System;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    [Serializable]
    public class OpenInteraction : Interaction
    {
        public event EventHandler<bool> OnOpenStateChanged;
        protected static readonly int OpenId = Animator.StringToHash("Open");

        private AttachedContainer _attachedContainer;

        public OpenInteraction() { }

        public OpenInteraction(AttachedContainer attachedContainer)
        {
            _attachedContainer = attachedContainer;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            Animator animator = ((IGameObjectProvider)interactionEvent.Target).GameObject.GetComponent<Animator>();
            if (_attachedContainer == null)
            {
                return animator.GetBool(OpenId) ? "Close" : "Open";
            }

            string name = _attachedContainer.ContainerName;

            return animator.GetBool(OpenId) ? "Close " + name : "Open " + name;

        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Icons.Get<Sprite>(InteractionIcons.Open);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // Check whether the object is in range
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            // Confirm that there is an entity doing this interaction
            Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();
            if (entity == null)
            {
                return false;
            }

            if (interactionEvent.Target is IGameObjectProvider target)
            {
                // Check that the entity is actually capable of interacting with the target
                if (entity.GetComponent<Hands>().SelectedHand.CanInteract(target.GameObject) && IsFirstContainerOpenable(target))
                {
                    return target.GameObject.GetComponent<Animator>() != null;
                }
            }
            return false;
        }

        /// <summary>
        /// Verifies if the attachedContainer referenced by this script is the first one on the game object at the source of the interaction.
        /// </summary>
        private bool IsFirstContainerOpenable(IGameObjectProvider target)
        {
            // Only accept the first Openable container on the GameObject.
            // Note: if you want separately functioning doors etc, they must be on different GameObjects.
            var attachedContainers = target.GameObject.GetComponents<ContainerInteractive>();
            for (int i = 0; i < attachedContainers.Length; i++)
            {

                if (_attachedContainer != attachedContainers[i].attachedContainer && attachedContainers[i].attachedContainer.IsOpenable)
                {
                    return false;
                }

                if (_attachedContainer == attachedContainers[i].attachedContainer)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Debug.Log("in OpenInteraction, Start");
            GameObject target = ((IGameObjectProvider) interactionEvent.Target).GameObject;
            Animator animator = target.GetComponent<Animator>();
            bool open = animator.GetBool(OpenId);
            animator.SetBool(OpenId, !open);
            OnOpenStateChange(!open);
            return false;
        }

        private void OnOpenStateChange(bool e)
        {
            Debug.Log("In OpenInteraction, OnOpenStateChange");
            OnOpenStateChanged?.Invoke(this, e);
        }
    }
}