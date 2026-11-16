using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SS3D.Data.Persistence;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace AssetAudit
{
    /// <summary>
    /// A saved object's setup entries are found again by key, so two setup components on one object must not share one
    /// (Documents/architecture/2026-11_object-setup-persistence.md Q13). Containers are the case that can drift: their key
    /// comes from the child they sit on, so two child containers with the same name would lose one's contents on load.
    /// </summary>
    public sealed class ObjectSetupAuditTests
    {
        [Test]
        public void SetupKeysAreUniqueWithinEachObject()
        {
            StringBuilder failures = new();
            foreach (GameObject prefab in AssetAuditUtilities.AllPrefabs())
            {
                IPersistentSetup[] components = prefab.GetComponentsInChildren<IPersistentSetup>(true);
                if (components.Length < 2)
                    continue;

                // Grouped by the object whose record holds them: a stored item's components are its own.
                Dictionary<(Item Owner, string Key), int> seen = new();
                foreach (IPersistentSetup setup in components)
                {
                    Item owner = ((Component)setup).GetComponentInParent<Item>(true);
                    (Item, string) slot = (owner, setup.SetupKey);
                    seen[slot] = seen.TryGetValue(slot, out int count) ? count + 1 : 1;
                }

                foreach (KeyValuePair<(Item Owner, string Key), int> pair in seen)
                {
                    if (pair.Value > 1)
                        failures.AppendLine($"{prefab.name}: {pair.Value} setup components keyed '{pair.Key.Key}'");
                }
            }

            Assert.IsEmpty(failures.ToString(),
                "Rename the duplicate child containers (a tier-B recipe, not hand-edited YAML) so each key is unique");
        }

        [Test]
        public void ContainerNamesAreUniqueOnObjectsWithSeveralContainers()
        {
            StringBuilder failures = new();
            foreach (GameObject prefab in AssetAuditUtilities.AllPrefabs())
            {
                AttachedContainer[] containers = prefab.GetComponentsInChildren<AttachedContainer>(true);
                if (containers.Length < 2)
                    continue;

                HashSet<string> names = new();
                foreach (AttachedContainer container in containers)
                {
                    if (!names.Add(container.ContainerName))
                        failures.AppendLine($"{prefab.name}: two containers named '{container.ContainerName}'");
                }
            }

            Assert.IsEmpty(failures.ToString());
        }
    }
}
