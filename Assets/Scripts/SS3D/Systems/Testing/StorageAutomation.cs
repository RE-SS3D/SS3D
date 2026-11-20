using FishNet;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections;
using UnityEngine;

namespace SS3D.Systems.Testing
{
    /// <summary>
    /// Automation steps for mppm:open-storage (#1531): a remote client opens its own worn storage through the
    /// server, the same request the HUD open button sends.
    /// </summary>
    public static class StorageAutomation
    {
        /// <summary>Client: opens the worn bag (else a pocket) and waits for the server to open and then close it.</summary>
        public static IEnumerator AssertOpenWornStorage(float timeoutSeconds)
        {
            yield return Wait(() => SubSystems.Get<EntitySubSystem>().TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out _),
                timeoutSeconds, "assert_open_storage embarked");
            SubSystems.Get<EntitySubSystem>().TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity);
            HumanInventory inventory = entity.GetComponent<HumanInventory>();
            ContainerViewer viewer = inventory.containerViewer;

            AttachedContainer storage = null;
            yield return Wait(() => TryFindWornStorage(inventory, out storage), timeoutSeconds, "assert_open_storage worn storage");

            bool opened = false;
            bool closed = false;
            ContainerViewer.ContainerEventHandler onOpened = container => opened |= container == storage;
            ContainerViewer.ContainerEventHandler onClosed = container => closed |= container == storage;
            viewer.OnContainerOpened += onOpened;
            viewer.OnContainerClosed += onClosed;
            try
            {
                viewer.CmdShowContainerUI(storage);
                yield return Wait(() => opened, timeoutSeconds, "assert_open_storage opened");

                viewer.CmdContainerClose(storage);
                yield return Wait(() => closed, timeoutSeconds, "assert_open_storage closed");
                TestSignal.Emit(viewer, "StorageOpened", storage.ContainerName);
            }
            finally
            {
                viewer.OnContainerOpened -= onOpened;
                viewer.OnContainerClosed -= onClosed;
            }
        }

        private static bool TryFindWornStorage(HumanInventory inventory, out AttachedContainer storage)
        {
            if (inventory.TryGetTypeContainer(ContainerType.Bag, 0, out AttachedContainer back))
            {
                foreach (Item item in back.Items)
                {
                    if (ItemStorage.TryGetStorageOn(item, out storage))
                    {
                        return true;
                    }
                }
            }

            return inventory.TryGetTypeContainer(ContainerType.Pocket, 0, out storage);
        }

        private static IEnumerator Wait(Func<bool> condition, float timeoutSeconds, string label)
        {
            float start = Time.time;
            while (!condition())
            {
                if (Time.time - start > timeoutSeconds)
                {
                    throw new TimeoutException($"Timed out after {timeoutSeconds}s waiting for {label}");
                }

                yield return null;
            }
        }
    }
}
