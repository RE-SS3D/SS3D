using FishNet;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Persistence;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// A container's contents as authored setup (Documents/architecture/2026-11_object-setup-persistence.md Q13): what a
    /// mapper put in a locker, crate or bag stays there in the saved station. Each stored item carries its own setup, so a
    /// box in a bag in a locker comes back whole; every level is a string inside its parent's.
    /// </summary>
    public static class ContainerContentsSetup
    {
        private const string KeyPrefix = "container";
        private const int MaxNesting = 16;

        /// <summary>One stored item: the asset to spawn, where it sat, its stack size and its own setup.</summary>
        [Serializable]
        public struct SavedContainedItem
        {
            /// <summary>The item asset's prefab id; loading tries it first, like a placed item.</summary>
            public string id;

            public string itemName;

            public Vector2Int position;

            /// <summary>The stack's size; 1 for an item that doesn't stack.</summary>
            public int count;

            public SavedSetupEntry[] setup;
        }

        [Serializable]
        public sealed class SavedContents
        {
            public SavedContainedItem[] items = Array.Empty<SavedContainedItem>();
        }

        /// <summary>
        /// "container" for a container on its object's root, "container:&lt;child name&gt;" for one on a child. Unique
        /// within an object while its child containers have distinct names; the asset audit checks that.
        /// </summary>
        public static string KeyFor(AttachedContainer container)
        {
            GameObject owner = OwnerOf(container);
            return owner == null || owner == container.gameObject ? KeyPrefix : $"{KeyPrefix}:{TrimClone(container.gameObject.name)}";
        }

        public static bool IsContainerKey(string key) =>
            key == KeyPrefix || (key != null && key.StartsWith(KeyPrefix + ":", StringComparison.Ordinal));

        /// <summary>Null for an empty container, which is the default: no mechanism gives containers starting contents.</summary>
        public static string Capture(AttachedContainer container)
        {
            SubSystems.TryGet(out TileSubSystem tiles);
            List<SavedContainedItem> items = null;
            foreach (Item item in container.Items)
            {
                if (item != null)
                    (items ??= new List<SavedContainedItem>()).Add(CaptureItem(item, container.PositionOf(item), tiles));
            }

            return items == null ? null : JsonUtility.ToJson(new SavedContents { items = items.ToArray() });
        }

        /// <summary>
        /// Server: replaces the contents with the saved ones. A no-op when they already match, so a second call leaves the
        /// same items in place. An item whose asset this build doesn't have is counted in the load report and left out.
        /// </summary>
        public static void Restore(AttachedContainer container, string json)
        {
            SavedContents saved = string.IsNullOrEmpty(json) ? new SavedContents() : JsonUtility.FromJson<SavedContents>(json);
            if (saved == null)
                throw new ArgumentException("container contents JSON is empty");

            saved.items ??= Array.Empty<SavedContainedItem>();
            if (Capture(container) == (saved.items.Length == 0 ? null : JsonUtility.ToJson(saved)))
                return;

            DeleteContents(container);
            SubSystems.TryGet(out TileSubSystem tiles);
            foreach (SavedContainedItem record in saved.items)
                RestoreItem(container, record, tiles);
        }

        /// <summary>
        /// The asset names of every item a saved setup would put into containers, nested ones included; for checks that
        /// must see a copy's whole contents (the sandbox's denied assets).
        /// </summary>
        public static void CollectItemNames(IReadOnlyList<SavedSetupEntry> setup, List<string> into, int depth = 0)
        {
            if (setup == null || depth > MaxNesting)
                return;

            foreach (SavedSetupEntry entry in setup)
            {
                if (!IsContainerKey(entry.key) || !TryParse(entry.json, out SavedContents contents))
                    continue;

                foreach (SavedContainedItem item in contents.items)
                {
                    into.Add(item.itemName);
                    CollectItemNames(item.setup, into, depth + 1);
                }
            }
        }

        private static bool TryParse(string json, out SavedContents contents)
        {
            contents = null;
            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                contents = JsonUtility.FromJson<SavedContents>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return contents?.items != null;
        }

        private static SavedContainedItem CaptureItem(Item item, Vector2Int position, TileSubSystem tiles)
        {
            GenericObjectSo asset = tiles != null && item.Asset != null ? tiles.GetSavedAsset(item.Asset.Id, null) : null;
            return new SavedContainedItem
            {
                id = asset != null ? SavedAssetLookup.IdOf(asset) : item.Asset?.Id,
                itemName = asset != null ? asset.NameString : TrimClone(item.gameObject.name),
                position = position,
                count = Mathf.Max(1, item.StackCount),
                setup = ObjectSetup.Capture(item.gameObject),
            };
        }

        private static void RestoreItem(AttachedContainer container, SavedContainedItem record, TileSubSystem tiles)
        {
            if (tiles == null || tiles.GetSavedAsset(record.id, record.itemName) is not ItemObjectSo itemSo)
            {
                Log.Warning(container, "Skipping unknown or removed item asset '{assetName}' stored in {container}",
                    Logs.ServerOnly, record.itemName, container.gameObject.name);
                ObjectSetup.ReportSkipped(string.IsNullOrEmpty(record.itemName) ? "(unnamed)" : record.itemName);
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(Assets.Get<GameObject>(itemSo.PrefabAsset),
                container.transform.position, Quaternion.identity);
            InstanceFinder.ServerManager.Spawn(instance);
            if (!instance.TryGetComponent(out Item item))
            {
                InstanceFinder.ServerManager.Despawn(instance);
                ObjectSetup.ReportSkipped(itemSo.NameString);
                return;
            }

            if (item.IsStackable)
                item.SetStackCount(record.count);

            ObjectSetup.Restore(instance, record.setup, itemSo.NameString);

            // A stack re-merges into a matching one on add; a slot that no longer fits falls back to the first free one.
            if (container.AddItemPosition(item, record.position) || container.AddItem(item))
                return;

            Log.Warning(container, "Saved item {item} no longer fits in {container}; left out",
                Logs.ServerOnly, itemSo.NameString, container.gameObject.name);
            ObjectSetup.ReportSkipped(itemSo.NameString);
            DespawnWithContents(item);
        }

        /// <summary>
        /// Server: despawns everything stored in <paramref name="root"/>'s own containers before the object itself goes,
        /// so the stored items leave the network cleanly instead of being destroyed as children of a despawned object.
        /// </summary>
        public static void DeleteAllContents(GameObject root)
        {
            Item rootItem = root.GetComponent<Item>();
            foreach (AttachedContainer container in root.GetComponentsInChildren<AttachedContainer>(true))
            {
                if (container.GetComponentInParent<Item>(true) == rootItem)
                    DeleteContents(container);
            }
        }

        /// <summary>Server: despawns everything stored here, nested contents first, so no stored item outlives its holder.</summary>
        private static void DeleteContents(AttachedContainer container)
        {
            foreach (Item item in container.ServerTakeAllForSetup())
                DespawnWithContents(item);
        }

        private static void DespawnWithContents(Item item)
        {
            foreach (AttachedContainer nested in item.GetComponentsInChildren<AttachedContainer>(true))
            {
                if (nested.GetComponentInParent<Item>(true) == item)
                    DeleteContents(nested);
            }

            if (item.GameObject != null)
                InstanceFinder.ServerManager.Despawn(item.GameObject);
        }

        private static GameObject OwnerOf(AttachedContainer container)
        {
            Item item = container.GetComponentInParent<Item>(true);
            if (item != null)
                return item.gameObject;

            PlacedTileObject tile = container.GetComponentInParent<PlacedTileObject>(true);
            return tile != null ? tile.gameObject : container.transform.root.gameObject;
        }

        private static string TrimClone(string objectName)
        {
            const string cloneSuffix = "(Clone)";
            return objectName.EndsWith(cloneSuffix, StringComparison.Ordinal)
                ? objectName[..^cloneSuffix.Length].TrimEnd()
                : objectName;
        }
    }
}
