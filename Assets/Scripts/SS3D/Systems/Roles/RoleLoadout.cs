using Coimbra;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Inventory.Containers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// The items that should spawn in the player inventory after embarking
    /// </summary>
    [CreateAssetMenu(fileName = "Loadout", menuName = "Roles/Loadout")]
    public class RoleLoadout : ScriptableObject
    {
        public ObjectAssetReference HandLeftAsset;
        public ObjectAssetReference HandRightAsset;

        public SerializableDictionary<ContainerType, ObjectAssetReference> EquipmentAssets;

        public List<ObjectAssetReference> PocketAssets = new();

        [Tooltip("Items spawned inside the storage of a worn loadout item (a filled toolbelt or backpack). "
            + "Slot is the EquipmentAssets key of the item that holds them.")]
        public List<LoadoutContents> EquipmentContents = new();

        [Tooltip("Items spawned inside the storage of the left hand item (a filled medkit).")]
        public List<ObjectAssetReference> HandLeftContents = new();

        [Tooltip("Items spawned inside the storage of the right hand item.")]
        public List<ObjectAssetReference> HandRightContents = new();

        /// <summary>
        /// The storage of a loadout item that contents spawn into: its first container with a storage panel,
        /// so a PDA's card slot never counts. Null when the item has no storage.
        /// </summary>
        public static AttachedContainer StorageOf(GameObject item)
        {
            if (item == null)
            {
                return null;
            }

            foreach (AttachedContainer container in item.GetComponentsInChildren<AttachedContainer>(true))
            {
                if (container.HasUi)
                {
                    return container;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The items that start inside the storage of one worn loadout item.
    /// </summary>
    [Serializable]
    public class LoadoutContents
    {
        [Tooltip("The EquipmentAssets key of the item whose storage holds these items.")]
        public ContainerType Slot;

        public List<ObjectAssetReference> Items = new();
    }
}
