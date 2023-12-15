using SS3D.Interactions.Interfaces;


public interface IRecipeIngredient : INetworkObjectProvider, IGameObjectProvider
{
    public void Consume();
}
