using System.Linq;
using System.Collections.Generic;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using FishNet.Object.Synchronizing;
using FishNet.Object;


namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Display clothes on player for all clients.
    /// </summary>
    public class ClothesDisplayer : NetworkActor
    {
        /// <summary>
        /// A small structure containing information regarding clothes on player, to help syncing them over the network.
        /// For each bodypart that can have clothing, it also contains information on the item to display, if it should show or not.
        /// </summary>
        private struct ClothDisplayData
        {
            public ClothType Type;
            public Item ClothToDisplay;

            public ClothDisplayData(ClothType type, Item clothToDisplay)
            {
                Type = type;
                ClothToDisplay = clothToDisplay;
            }
        }

        /// <summary>
        /// The inventory containing the player's clothing slots.
        /// </summary>
        public HumanInventory _inventory;

        /// <summary>
        /// Synced list of body part associated to the clothes on them.
        /// </summary>
        [SyncObject]
        private readonly SyncList<ClothDisplayData> _clothedBodyParts = new SyncList<ClothDisplayData>();

        private readonly HashSet<Renderer> _occludedRenderers = new();

        private void RefreshOcclusion()
        {
            foreach (Renderer renderer in _occludedRenderers)
            {
                if (renderer != null) renderer.forceRenderingOff = false;
            }
            _occludedRenderers.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (ClothDisplayData data in _clothedBodyParts)
            {
                if (data.ClothToDisplay == null || !data.ClothToDisplay.TryGetComponent(out Cloth cloth)
                    || cloth.OccludedBodyRenderers == null) continue;
                foreach (Renderer renderer in renderers)
                {
                    if (!cloth.OccludedBodyRenderers.Contains(renderer.name)) continue;
                    renderer.forceRenderingOff = true;
                    _occludedRenderers.Add(renderer);
                }
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _inventory.OnContainerContentChanged += HandleContainerContentChanged;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _clothedBodyParts.OnChange += ClothedBodyPartsOnChange;
            foreach (ClothDisplayData data in _clothedBodyParts)
            {
                ClothedBodyPartsOnChange(SyncListOperation.Add, 0, default, data, false);
            }
        }

        private bool TryGetClothedBodyPart(ClothType type, out ClothedBodyPart part)
        {
            part = GetComponentsInChildren<ClothedBodyPart>(true).FirstOrDefault(candidate => candidate.Type == type);
            return part != null;
        }

        public override void OnStopClient()
        {
            _clothedBodyParts.OnChange -= ClothedBodyPartsOnChange;
            base.OnStopClient();
        }

        public override void OnStopServer()
        {
            _inventory.OnContainerContentChanged -= HandleContainerContentChanged;
            base.OnStopServer();
        }

        /// <summary>
        /// Callback when the syncedList _clothedBodyParts changes. Update the displayed clothes on the player.
        /// </summary>
        private void ClothedBodyPartsOnChange(SyncListOperation op, int index, ClothDisplayData oldData, ClothDisplayData newData, bool asServer)
        {
            if (asServer)
                return;

            switch (op)
            {
                // Show the new cloth on the player
                case SyncListOperation.Add:
                    if (!TryGetClothedBodyPart(newData.Type, out ClothedBodyPart newClothedBodyPart) || !newData.ClothToDisplay.TryGetComponent(out Cloth newCloth))
                    {
                        Log.Error(this, "Error getting components to display cloth on player");

                        return;
                    }

                    if (!newClothedBodyPart.TryGetComponent(out SkinnedMeshRenderer meshRenderer))
                    {
                        Log.Warning(this, $"no skinned mesh renderer on game object {newClothedBodyPart}, can't display cloth");

                        return;
                    }

                    newClothedBodyPart.gameObject.SetActive(true);
                    meshRenderer.sharedMesh = newCloth.GetClothMesh(newClothedBodyPart.Type);
                    newClothedBodyPart.ApplyMaterials(meshRenderer, newCloth.WornMaterials);
                    // A new sharedMesh starts with zero shape-key weights; keep it fitted to the body.
                    if (TryGetComponent(out Human human))
                    {
                        human.ReapplyBodyMorphs();
                    }

                    break;

                // Stop displaying cloth on the player
                case SyncListOperation.RemoveAt:
                    if (TryGetClothedBodyPart(oldData.Type, out ClothedBodyPart oldBodyPart))
                    {
                        oldBodyPart.gameObject.SetActive(false);
                    }

                    break;
            }
            RefreshOcclusion();
        }

        /// <summary>
        /// When the content of a container change, check if it should display or remove display of some clothes.
        /// </summary>
        [Server]
        public void HandleContainerContentChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            // If it's not a cloth type container.
            // It'd be probably better to just create "cloth container" inheriting from container to easily test that.
            if (!container.TryGetComponent(out ClothContainer clothContainer))
            {
                return;
            }

            switch (type)
            {
                case ContainerChangeType.Add:
                    AddCloth(newItem, clothContainer);

                    break;
                case ContainerChangeType.Remove:
                    RemoveCloth(oldItem, clothContainer);

                    break;
            }
        }

        /// <summary>
        /// Adds a cloth to the synced list, making a few checks to find where to add it, if possible.
        /// </summary>
        /// <param name="item"> The item to add, it should have a Cloth component on it.</param>
        /// <param name="container">The cloth container in which the item is being added to.</param>
        [Server]
        private void AddCloth(Item item, ClothContainer container)
        {
            if (!item || !item.GetComponent<Cloth>())
            {
                return;
            }

            if (TryGetClothedBodyPart(container.ClothType, out _))
            {
                _clothedBodyParts.Add(new(container.ClothType, item));
            }
        }

        /// <summary>
        /// Remove a cloth from the synced list, check if it's there before removing.
        /// </summary>
        /// <param name="item">The item to add, it should have a Cloth component on it.</param>
        /// <param name="container">The cloth container in which the item is being removed from.</param>
        [Server]
        private void RemoveCloth(Item item, ClothContainer container)
        {
            if (!item || !item.GetComponent<Cloth>())
            {
                return;
            }

            ClothType containerClothType = container.ClothType;
            ClothDisplayData clothData = _clothedBodyParts.Find(data => data.Type == containerClothType);

            _clothedBodyParts.Remove(clothData);
        }
    }
}