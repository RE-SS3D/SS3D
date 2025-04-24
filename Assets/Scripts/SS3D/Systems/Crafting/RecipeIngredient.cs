using Coimbra;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Simple implementation of the IRecipeIngredient interface. Upon consumption of a crafting ingredient, simply despawn it.
    /// </summary>
    public class RecipeIngredient : NetworkActor, IRecipeIngredient
    {
        [Server]
        public void Consume()
        {
            if (TryGetComponent(out Item item) && item.IsInContainer())
            {
                item.Container.RemoveItem(item);
            }

            if (TryGetComponent(out PlacedTileObject tileObject))
            {
                SubSystems.Get<TileSubSystem>().CurrentMap.ClearTileObject(gameObject.transform.position, tileObject.Layer, tileObject.Direction);
                return;
            }

            NetworkObject.Despawn();
            gameObject.Dispose(true);
        }
    }
}
