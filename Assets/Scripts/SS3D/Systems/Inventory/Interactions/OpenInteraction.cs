using SS3D.Data;
using System;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    [Serializable]
    public class OpenInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;
        public event EventHandler<bool> OnOpenStateChanged;
        protected static readonly int OpenId = Animator.StringToHash("Open");

        private AttachedContainer _attachedContainer;

        public OpenInteraction() { }

        public OpenInteraction(AttachedContainer attachedContainer)
        {
            _attachedContainer = attachedContainer;
        }

        public int Priority => 25;

        public string GetName(InteractionEvent interactionEvent)
        {
            bool isOpen = GetOpenState(interactionEvent);

            if (_attachedContainer == null)
            {
                return isOpen ? "Close" : "Open";
            }

            string name = _attachedContainer.ContainerName;

            return isOpen ? "Close " + name : "Open " + name;
        }

        public string GetGenericName() => "Open";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Open);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
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

            if (_attachedContainer != null && !_attachedContainer.IsAccessibleBy(entity.GetComponent<HumanInventory>()))
            {
                return false;
            }

            if (interactionEvent.Target is IGameObjectProvider target)
            {
                // Check that the entity is actually capable of interacting with the target
                if (entity.GetComponent<Hands>().SelectedHand.CanInteract(target.GameObject) && IsFirstContainerOpenable(target))
                {
                    return target.GameObject.GetComponent<NetworkedOpenable>() != null
                        || target.GameObject.GetComponent<Animator>() != null;
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

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            GameObject target = ((IGameObjectProvider)interactionEvent.Target).GameObject;
            NetworkedOpenable networkedOpenable = target.GetComponent<NetworkedOpenable>();

            if (networkedOpenable != null)
            {
                networkedOpenable.SetOpenState(!networkedOpenable.IsOpen());
                return false;
            }

            Animator animator = target.GetComponent<Animator>();

            if (animator != null)
            {
                bool open = animator.GetBool(OpenId);
                animator.SetBool(OpenId, !open);
                OnOpenStateChange(!open);
            }

            return false;
        }

        private static bool GetOpenState(InteractionEvent interactionEvent)
        {
            GameObject target = ((IGameObjectProvider)interactionEvent.Target).GameObject;
            NetworkedOpenable networkedOpenable = target.GetComponent<NetworkedOpenable>();

            if (networkedOpenable != null)
            {
                return networkedOpenable.IsOpen();
            }

            Animator animator = target.GetComponent<Animator>();

            return animator != null && animator.GetBool(OpenId);
        }

        private void OnOpenStateChange(bool e)
        {
            OnOpenStateChanged?.Invoke(this, e);
        }
    }
}