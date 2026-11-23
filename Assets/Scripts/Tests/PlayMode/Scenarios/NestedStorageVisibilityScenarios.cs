using System.Collections;
using System.Linq;
using FishNet.Object;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Furniture;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>
    /// Items stored in a container that is itself stored in a placed container stay hidden on the host when the
    /// placed object's renderers are shown again (Playtest bug 6, 2026-11-18: a filled medkit in a locker showed
    /// its gauze and bike horn lying loose in the open locker).
    /// </summary>
    [Category(TestCategories.Scenarios)]
    public class NestedStorageVisibilityScenarios : ScenarioFixture
    {
        /// <summary>Open space well away from the test room.</summary>
        private static readonly Vector3 LockerCell = new(40f, 0f, 44f);

        private static TileSubSystem Tiles => SubSystems.Get<TileSubSystem>();

        // Guards: host renderer refresh on a placed tile object (PlacedTileObject.RefreshHostVisibility, FishNet
        // SetRenderersVisible) leaves stored items hidden · Not cheaper because: it needs a spawned, observed
        // NetworkObject on a client-host, which EditMode never has
        [UnityTest]
        public IEnumerator ShowingALockerAgain_KeepsItsStoredItemsHidden()
        {
            var tile = (TileObjectSo)Tiles.GetAsset("Locker");
            try
            {
                Assert.IsTrue(Tiles.CurrentMap.PlaceTileObject(tile, LockerCell, Direction.North, true, false, false, out GameObject placed),
                    "The locker places");
                yield return null;

                Locker locker = placed.GetComponent<Locker>();
                locker.IsOpen = true;
                AttachedContainer storage = placed.GetComponentsInChildren<AttachedContainer>().First();

                ItemSubSystem items = SubSystems.Get<ItemSubSystem>();
                Item medkit = items.SpawnItemInContainer(Items.Medkit, storage);
                Assert.IsTrue(ItemStorage.TryGetStorageOn(medkit, out AttachedContainer medkitStorage), "The medkit has storage");
                Item horn = items.SpawnItemInContainer(Items.BikeHorn, medkitStorage);
                yield return null;

                Assert.IsFalse(AnyRendererOn(medkit), "Precondition: the medkit is hidden in the locker");
                Assert.IsFalse(AnyRendererOn(horn), "Precondition: the horn is hidden in the medkit");

                // The host walks up to the locker: once it observes the locker, the locker refreshes its renderers.
                NetworkObject lockerObject = placed.GetComponent<NetworkObject>();
                Teleport(Body, LockerCell + Vector3.back);
                yield return WaitFor(() => lockerObject.Observers.Contains(lockerObject.NetworkManager.ClientManager.Connection),
                    "The host observes the locker");
                placed.GetComponent<PlacedTileObject>().RefreshHostVisibility();
                lockerObject.SetRenderersVisible(false, true);
                lockerObject.SetRenderersVisible(true, true);
                yield return null;

                Assert.IsTrue(AnyRendererOn(locker), "The locker itself shows");
                Assert.IsFalse(AnyRendererOn(medkit), "The medkit stays hidden in the locker");
                Assert.IsFalse(AnyRendererOn(horn), "The horn stays hidden in the medkit");

                // Taking the medkit out shows it again, but not what is still stored inside it.
                EmptyHands(Body);
                storage.RemoveItem(medkit);
                Assert.IsTrue(Body.GetComponent<Hands>().SelectedHand.Container.AddItem(medkit), "The medkit goes in the hand");
                yield return null;
                Assert.IsTrue(AnyRendererOn(medkit), "The medkit shows in the hand");
                Assert.IsFalse(AnyRendererOn(horn), "The horn stays hidden in the held medkit");
            }
            finally
            {
                Tiles.CurrentMap.ClearTileObject(LockerCell, tile.layer, Direction.North);
            }
        }

        /// <summary>Whether any of the renderers that belong to <paramref name="owner"/> itself (not to items stored in it) draws.</summary>
        private static bool AnyRendererOn(Component owner)
        {
            Item self = owner as Item;
            return owner.GetComponentsInChildren<Renderer>(true).Any(r =>
                r.enabled
                && !r.forceRenderingOff
                && !InteractionOutlineView.IsOutlineRenderer(r)
                && (self == null ? r.GetComponentInParent<Item>() == null : r.GetComponentInParent<Item>() == self));
        }
    }
}
