using Coimbra;
using FishNet.Object;
using SS3D.Core.Behaviours;

/// <summary>
/// Simple implementation of the IRecipeIngredient interface. Upon consumption of a crafting ingredient, simply despawn it.
/// </summary>
public class RecipeIngredient : NetworkActor, IRecipeIngredient
{
    [Server]
    public void Consume()
    {
        NetworkObject.Despawn();
        gameObject.Dispose(true);
    }
}
