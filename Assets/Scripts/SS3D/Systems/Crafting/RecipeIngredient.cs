using Coimbra;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Items;

/// <summary>
/// Simple implementation of the IRecipeIngredient interface. Upon consumption of a crafting ingredient, simply despawn it.
/// </summary>
public class RecipeIngredient : NetworkActor, IRecipeIngredient
{
    [Server]
    public void Consume()
    {
        if (TryGetComponent(out Item item)  && item.IsInContainer())
        {
            item.Container.RemoveItem(item);
        }
        NetworkObject.Despawn();
        gameObject.Dispose(true);
    }
}
