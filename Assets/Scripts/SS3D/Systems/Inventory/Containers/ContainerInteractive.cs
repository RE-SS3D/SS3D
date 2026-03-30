using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Interactions;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// This allow a container to send back container related possible interactions,
    /// including viewing the content, storing, opening and others.
    /// It also handle some UI stuff, such as closing the UI for all clients when someone close the container.
    /// </summary>
    public class ContainerInteractive : NetworkedOpenable
    {
        public AttachedContainer attachedContainer;
        private Sprite _viewContainerIcon;

        private AssetHandle<Sprite> _takeIconHandle;
        private AssetHandle<Sprite> _openIconHandle;

        protected override void OnAwake()
        {
            base.OnAwake();
            AcquireAssets();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            ReleaseAssets();
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            if (attachedContainer.HasCustomInteraction)
            {
                return Array.Empty<IInteraction>();
            }

            List<IInteraction> interactions = new();

            StoreInteraction storeInteraction = new(attachedContainer)
            {
                Icon = _takeIconHandle?.Asset,
            };

            TakeFirstInteraction takeFirstInteraction = new(attachedContainer)
            {
                Icon = _takeIconHandle?.Asset,
            };

            ViewContainerInteraction view = new(attachedContainer)
            {
                MaxDistance = attachedContainer.MaxDistance, Icon = _openIconHandle?.Asset,
            };

            // Pile or Normal the Store Interaction will always appear, but View only appears in Normal containers
            if (IsOpen() | !attachedContainer.OnlyStoreWhenOpen | !attachedContainer.IsOpenable)
            {
                if (attachedContainer.HasUi)
                {
                    interactions.Add(storeInteraction);
                    interactions.Add(view);
                }
                else
                {
                    interactions.Add(storeInteraction);
                    interactions.Add(takeFirstInteraction);
                }
            }

            if (!attachedContainer.IsOpenable)
            {
                return interactions.ToArray();
            }

            OpenInteraction openInteraction = new(attachedContainer)
            {
                Icon = _openIconHandle?.Asset,
            };

            openInteraction.OnOpenStateChanged += OpenStateChanged;
            interactions.Add(openInteraction);

            return interactions.ToArray();
        }

        protected override void OpenStateChanged(object sender, bool e)
        {
            base.OpenStateChanged(sender, e);
        }

        protected override void SyncOpenState(bool oldVal, bool newVal, bool asServer)
        {
            base.SyncOpenState(oldVal, newVal, asServer);
            if (!newVal)
            {
                CloseUis();
            }
        }

        /// <summary>
        /// Recursively closes all container UI when the root container is closed.
        /// This is potentially very slow when there's a lot of containers and items as it calls get component for every items in every container.
        /// A faster solution could be to use unity game tag and to tag every object with a container as such.
        /// Keeping track in Container of the list of objects that are containers would make it really fast.
        /// </summary>
        private void CloseUis()
        {
            if (attachedContainer.ContainerUi != null)
            {
                attachedContainer.ContainerUi.Close();
            }

            // We check for each item if they are interactive containers.
            foreach(Item item in attachedContainer.Items)
            {
                ContainerInteractive[] containerInteractives = item.GameObject.GetComponents<ContainerInteractive>();
                // If the item is an interactive container, we call this method again on it.
                if (containerInteractives == null)
                {
                    continue;
                }

                foreach(ContainerInteractive c in containerInteractives)
                {
                    c.CloseUis();
                }
            }
        }

        private async void AcquireAssets()
        {
            _takeIconHandle = await new AssetRequest<Sprite>(InteractionIcons.Take).ExecuteAsync();
            _openIconHandle = await new AssetRequest<Sprite>(InteractionIcons.Open).ExecuteAsync();

            if (!_takeIconHandle)
            {
                _takeIconHandle?.Dispose();
                _takeIconHandle = null;
            }

            if (!_openIconHandle)
            {
                _openIconHandle?.Dispose();
                _openIconHandle = null;
            }
        }

        private void ReleaseAssets()
        {
            _takeIconHandle?.Dispose();
            _openIconHandle?.Dispose();
            
            _takeIconHandle = null;
            _openIconHandle = null;
        }
    }
}